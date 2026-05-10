# GraphRAG Usage Examples

## Basic Vector Search

```csharp
var vectorIndex = serviceProvider.GetRequiredService<IVectorIndex>();
var embeddingService = serviceProvider.GetRequiredService<IEmbeddingService>();

// 1. Embed user query
var userQuery = "How do I authenticate users?";
var queryEmbedding = await embeddingService.GetEmbeddingAsync(userQuery);

// 2. Search for similar code
var results = await vectorIndex.SearchAsync(queryEmbedding, topK: 5);

// 3. Display results
foreach (var result in results)
{
    Console.WriteLine($"[{result.Score:F3}] {result.ClassName}.{result.Type}");
    Console.WriteLine($"File: {result.FilePath}");
    Console.WriteLine($"Code:\n{result.Content}\n");
}
```

**Output:**
```
[0.892] AuthenticationService.AuthenticateUser
File: src/Auth/AuthenticationService.cs
Code:
public async Task<bool> AuthenticateUser(string username, string password)
{
    // ... authentication logic
}

[0.845] UserController.Login
File: src/API/Controllers/UserController.cs
Code:
[HttpPost("login")]
public async Task<IActionResult> Login(LoginRequest request)
{
    // ... login endpoint
}
```

---

## GraphRAG: Vector + Graph Traversal

```csharp
// Cast to Neo4jVectorIndex to access GraphRAG methods
var neo4jVectorIndex = (Neo4jVectorIndex)vectorIndex;

// Perform GraphRAG search
var graphRAGResults = await neo4jVectorIndex.GraphRAGSearchAsync(
    queryEmbedding,
    topK: 5,
    maxHops: 2  // Traverse 2 levels of relationships
);

// Display enriched results with context
foreach (var match in graphRAGResults.Matches)
{
    Console.WriteLine($"\n[{match.Score:F3}] {match.EntityName} ({match.EntityType})");
    Console.WriteLine($"File: {match.FilePath}");
    Console.WriteLine($"\nCode:\n{match.Content}");

    if (match.RelatedEntities.Any())
    {
        Console.WriteLine($"\nRelated Entities ({string.Join(", ", match.RelationshipTypes)}):");
        foreach (var related in match.RelatedEntities)
        {
            Console.WriteLine($"  - {related.Name} ({related.Type})");
        }
    }
}
```

**Output:**
```
[0.892] AuthenticateUser (Method)
File: src/Auth/AuthenticationService.cs

Code:
public async Task<bool> AuthenticateUser(string username, string password)
{
    var user = await _userRepository.GetByUsername(username);
    return await _passwordHasher.VerifyPassword(user, password);
}

Related Entities (CALLS, DEPENDS_ON):
  - UserRepository.GetByUsername (Method)
  - PasswordHasher.VerifyPassword (Method)
  - User (Class)
  - IUserRepository (Class)
```

---

## Filtered GraphRAG Search

### Example 1: Find code in a specific namespace

```csharp
// Custom Cypher filter for namespace
var cypherFilter = @"
    MATCH (startNode)-[:REPRESENTS]->(entity)
    WHERE entity:Method OR entity:Class
    MATCH (entity)<-[:HAS_METHOD|HAS_CLASS*]-(ns:Namespace)
    WHERE ns.name STARTS WITH 'Legacy.API'
";

// Execute with custom Neo4j session
await using var session = neo4jVectorIndex.Driver.AsyncSession();
var results = await session.ExecuteReadAsync(async tx =>
{
    var result = await tx.RunAsync($@"
        CALL db.index.vector.queryNodes('code_embeddings', 10, $queryVector)
        YIELD node AS startNode, score

        {cypherFilter}

        RETURN startNode, score, entity, ns.name as namespace
        ORDER BY score DESC
        LIMIT 5
    ", new { queryVector = queryEmbedding });

    // Process results...
});
```

### Example 2: Find methods that call external APIs

```csharp
var cypherQuery = @"
    CALL db.index.vector.queryNodes('code_embeddings', 20, $queryVector)
    YIELD node AS startNode, score

    MATCH (startNode)-[:REPRESENTS]->(method:Method)
    WHERE method.name CONTAINS 'Api' OR method.name CONTAINS 'Http'

    OPTIONAL MATCH (method)-[:CALLS]->(externalCall:Method)
    WHERE externalCall.namespace STARTS WITH 'System.Net' 
       OR externalCall.namespace STARTS WITH 'RestSharp'

    RETURN 
        startNode.content as code,
        score,
        method.name as methodName,
        method.namespace as namespace,
        collect(externalCall.name) as externalAPIs
    ORDER BY score DESC
    LIMIT 5
";
```

---

## Hybrid Search (Vector + Business Logic)

```csharp
public class AriadnaKnowledgeStoreService
{
    private readonly Neo4jVectorIndex _vectorIndex;
    private readonly IEmbeddingService _embeddingService;

    public async Task<List<EnrichedCodeResult>> FindCodeWithContext(
        string userQuery,
        string? filterNamespace = null,
        bool includeTests = false)
    {
        // 1. Embed query
        var embedding = await _embeddingService.GetEmbeddingAsync(userQuery);

        // 2. GraphRAG search
        var graphRAGResult = await _vectorIndex.GraphRAGSearchAsync(
            embedding,
            topK: 10,
            maxHops: 2
        );

        // 3. Apply business filters
        var filtered = graphRAGResult.Matches
            .Where(m => includeTests || !m.FilePath.Contains("Test"))
            .Where(m => filterNamespace == null || m.ClassName.StartsWith(filterNamespace))
            .ToList();

        // 4. Build enriched results
        var enrichedResults = filtered.Select(match => new EnrichedCodeResult
        {
            Code = match.Content,
            FilePath = match.FilePath,
            EntityName = match.EntityName,
            SimilarityScore = match.Score,
            Dependencies = match.RelatedEntities
                .Where(e => match.RelationshipTypes.Contains("DEPENDS_ON"))
                .Select(e => e.Name)
                .ToList(),
            Callers = match.RelatedEntities
                .Where(e => match.RelationshipTypes.Contains("CALLS"))
                .Select(e => e.Name)
                .ToList()
        }).ToList();

        return enrichedResults;
    }
}

public class EnrichedCodeResult
{
    public string Code { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
    public List<string> Dependencies { get; set; } = new();
    public List<string> Callers { get; set; } = new();
}
```

**Usage:**
```csharp
var service = new AriadnaKnowledgeStoreService(vectorIndex, embeddingService);

var results = await service.FindCodeWithContext(
    userQuery: "database connection pooling",
    filterNamespace: "Legacy.DataAccess",
    includeTests: false
);

foreach (var result in results)
{
    Console.WriteLine($"{result.EntityName} [{result.SimilarityScore:F3}]");
    Console.WriteLine($"Dependencies: {string.Join(", ", result.Dependencies)}");
    Console.WriteLine($"Called by: {string.Join(", ", result.Callers)}");
    Console.WriteLine();
}
```

---

## Advanced: Multi-Hop Traversal

```csharp
// Find all methods that eventually call a specific service
var cypherQuery = @"
    // 1. Vector search for user query
    CALL db.index.vector.queryNodes('code_embeddings', 10, $queryVector)
    YIELD node AS startNode, score

    MATCH (startNode)-[:REPRESENTS]->(startMethod:Method)

    // 2. Find all paths to DatabaseService
    MATCH path = (startMethod)-[:CALLS*1..5]->(dbService:Method)
    WHERE dbService.className = 'DatabaseService'

    // 3. Extract intermediate calls
    WITH startNode, score, startMethod, dbService, 
         [node in nodes(path) | node.name] as callChain

    RETURN 
        startNode.content as code,
        score,
        startMethod.name as startingMethod,
        dbService.name as targetMethod,
        callChain,
        length(callChain) as hops
    ORDER BY score DESC, hops ASC
    LIMIT 5
";

await using var session = vectorIndex.Driver.AsyncSession();
var results = await session.ExecuteReadAsync(async tx =>
{
    var result = await tx.RunAsync(cypherQuery, new { queryVector = embedding });

    var matches = new List<MultiHopResult>();
    await foreach (var record in result)
    {
        matches.Add(new MultiHopResult
        {
            Code = record["code"].As<string>(),
            Score = record["score"].As<double>(),
            StartMethod = record["startingMethod"].As<string>(),
            TargetMethod = record["targetMethod"].As<string>(),
            CallChain = record["callChain"].As<List<string>>(),
            Hops = record["hops"].As<int>()
        });
    }

    return matches;
});
```

**Output:**
```
[0.876] UserController.CreateUser → DatabaseService.Insert (3 hops)
Call chain: CreateUser → ValidateUser → SaveUser → Insert

[0.845] OrderService.ProcessOrder → DatabaseService.BeginTransaction (4 hops)
Call chain: ProcessOrder → CreateOrder → SaveOrder → ExecuteTransaction → BeginTransaction
```

---

## Performance Tips

### 1. Warm Up Vector Index

```csharp
// On application startup
await vectorIndex.EnsureIndexAsync(CancellationToken.None);
```

### 2. Batch Queries

```csharp
var queries = new[] { "authentication", "database", "caching" };
var embeddings = await Task.WhenAll(
    queries.Select(q => embeddingService.GetEmbeddingAsync(q))
);

var results = await Task.WhenAll(
    embeddings.Select(e => vectorIndex.SearchAsync(e, topK: 5))
);
```

### 3. Limit Traversal Depth

```csharp
// maxHops = 1 is usually sufficient and much faster
var results = await vectorIndex.GraphRAGSearchAsync(
    embedding,
    topK: 5,
    maxHops: 1  // ✅ Faster, still provides good context
);
```

### 4. Use Projection

```cypher
// Only return necessary fields
RETURN 
    node.id,
    node.content,
    score
// ❌ AVOID: RETURN *
```

---

## Integration with LLMs

```csharp
public async Task<string> AnswerCodeQuestion(string userQuestion)
{
    // 1. GraphRAG search
    var embedding = await _embeddingService.GetEmbeddingAsync(userQuestion);
    var graphRAGResults = await _vectorIndex.GraphRAGSearchAsync(embedding, topK: 3, maxHops: 1);

    // 2. Build context for LLM
    var context = string.Join("\n\n---\n\n", graphRAGResults.Matches.Select(match =>
        $"File: {match.FilePath}\n" +
        $"Entity: {match.EntityName} ({match.EntityType})\n" +
        $"Code:\n{match.Content}\n" +
        $"Related: {string.Join(", ", match.RelatedEntities.Select(e => e.Name))}"
    ));

    // 3. Send to LLM
    var prompt = $@"
You are a code analysis assistant. Answer the user's question using the provided code context.

Context:
{context}

User Question: {userQuestion}

Answer:";

    var answer = await _llmService.CompleteAsync(prompt);
    return answer;
}
```

**Example:**
```
User: "How does authentication work in this codebase?"

Context:
File: src/Auth/AuthenticationService.cs
Entity: AuthenticateUser (Method)
Code:
public async Task<bool> AuthenticateUser(string username, string password) { ... }
Related: UserRepository.GetByUsername, PasswordHasher.VerifyPassword

---

File: src/API/Middleware/AuthenticationMiddleware.cs
Entity: Invoke (Method)
Code:
public async Task Invoke(HttpContext context) { ... }
Related: AuthenticationService.AuthenticateUser, TokenService.ValidateToken

LLM Answer:
"The authentication system uses a two-step process:
1. AuthenticationService.AuthenticateUser validates credentials by...
2. AuthenticationMiddleware intercepts requests and validates tokens using..."
```

---

## Summary

✅ **Simple Vector Search**: When you just need similar code chunks  
✅ **GraphRAG**: When you need context (dependencies, callers, relationships)  
✅ **Custom Filters**: When you need business logic constraints  
✅ **Multi-Hop**: When you need deep traversal (call chains, dependency trees)

**All powered by Neo4j in a single query!** 🚀
