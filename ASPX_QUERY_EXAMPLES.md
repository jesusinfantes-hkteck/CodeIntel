# Ejemplos de Consultas ASPX en Neo4j

Este documento proporciona ejemplos prácticos de consultas Cypher para explorar elementos ASPX en el grafo de código.

## 📋 Consultas Básicas

### 1. Listar todas las páginas ASPX
```cypher
MATCH (p:AspxPage)
RETURN p.name, p.codeBehindClass, p.filePath
ORDER BY p.name
```

### 2. Contar elementos ASPX por tipo
```cypher
MATCH (p:AspxPage)
RETURN count(p) as totalPages

UNION

MATCH (c:AspxControl)
RETURN count(c) as totalControls

UNION

MATCH (e:AspxEvent)
RETURN count(e) as totalEvents
```

### 3. Encontrar páginas con su clase code-behind
```cypher
MATCH (page:AspxPage)-[:CODE_BEHIND]->(class:Class)
RETURN page.name, class.name, class.namespace
ORDER BY page.name
```

## 🔍 Consultas de Controles

### 4. Listar controles por página
```cypher
MATCH (page:AspxPage)-[:HAS_CONTROL]->(control:AspxControl)
RETURN page.name as Page, 
       collect(control.name) as Controls,
       count(control) as ControlCount
ORDER BY ControlCount DESC
```

### 5. Encontrar todos los botones (Button controls)
```cypher
MATCH (page:AspxPage)-[:HAS_CONTROL]->(control:AspxControl)
WHERE control.type =~ '(?i).*button.*'
RETURN page.name, control.name, control.type, control.events
```

### 6. Controles sin eventos
```cypher
MATCH (control:AspxControl)
WHERE control.events IS NULL OR control.events = ''
RETURN control.name, control.type, control.pageId
```

## 🎯 Consultas de Eventos

### 7. Todos los eventos OnClick
```cypher
MATCH (control:AspxControl)-[:TRIGGERS]->(event:AspxEvent)
WHERE event.eventName = 'OnClick'
RETURN event.controlId, event.handlerMethod
```

### 8. Mapeo completo Control → Evento → Método
```cypher
MATCH (page:AspxPage)-[:HAS_CONTROL]->(control:AspxControl)
      -[:TRIGGERS]->(event:AspxEvent)
      -[:HANDLES_EVENT]->(method:Method)
RETURN page.name as Page,
       control.name as Control,
       event.eventName as Event,
       method.name as Handler
ORDER BY page.name, control.name
```

### 9. Páginas más interactivas (más eventos)
```cypher
MATCH (page:AspxPage)-[:HAS_CONTROL]->(control:AspxControl)
      -[:TRIGGERS]->(event:AspxEvent)
RETURN page.name, page.filePath, count(event) as EventCount
ORDER BY EventCount DESC
LIMIT 10
```

## 🔗 Consultas de Flujo de Ejecución

### 10. Rastrear desde página hasta llamadas de métodos (profundidad 3)
```cypher
MATCH path = (page:AspxPage {name: 'Default.aspx'})
             -[:HAS_CONTROL]->(control)
             -[:TRIGGERS]->(event)
             -[:HANDLES_EVENT]->(method)
             -[:CALLS*0..3]->(called)
RETURN path
```

### 11. Encontrar todos los métodos invocados desde una página
```cypher
MATCH (page:AspxPage {name: 'Login.aspx'})
      -[:HAS_CONTROL]->()
      -[:TRIGGERS]->()
      -[:HANDLES_EVENT]->(handler:Method)
      -[:CALLS*1..5]->(invoked:Method)
RETURN DISTINCT page.name, handler.name as EntryPoint, invoked.name as InvokedMethod
```

### 12. Detectar patrones de validación (controles + métodos)
```cypher
MATCH (page:AspxPage)-[:HAS_CONTROL]->(control:AspxControl)
      -[:TRIGGERS]->(event:AspxEvent)
      -[:HANDLES_EVENT]->(method:Method)
WHERE control.type =~ '(?i).*validator.*'
   OR method.name =~ '(?i).*validate.*'
RETURN page.name, control.name, method.name
```

## 🚨 Consultas de Diagnóstico

### 13. Páginas huérfanas (sin code-behind)
```cypher
MATCH (page:AspxPage)
WHERE NOT EXISTS((page)-[:CODE_BEHIND]->(:Class))
RETURN page.name, page.filePath, page.inherits
```

### 14. Eventos sin handler (método no encontrado)
```cypher
MATCH (event:AspxEvent)
WHERE NOT EXISTS((event)-[:HANDLES_EVENT]->(:Method))
RETURN event.eventName, event.handlerMethod, event.controlId
```

### 15. Controles con muchos eventos (complejidad)
```cypher
MATCH (control:AspxControl)-[:TRIGGERS]->(event:AspxEvent)
WITH control, count(event) as eventCount
WHERE eventCount > 3
RETURN control.name, control.type, control.pageId, eventCount
ORDER BY eventCount DESC
```

### 16. Clases code-behind más referenciadas
```cypher
MATCH (class:Class)<-[:CODE_BEHIND]-(page:AspxPage)
RETURN class.name, class.namespace, count(page) as PageCount
ORDER BY PageCount DESC
```

## 📊 Análisis de Arquitectura

### 17. Páginas que comparten la misma clase code-behind
```cypher
MATCH (page1:AspxPage)-[:CODE_BEHIND]->(class:Class)<-[:CODE_BEHIND]-(page2:AspxPage)
WHERE id(page1) < id(page2)
RETURN class.name, collect(page1.name) + collect(page2.name) as Pages
```

### 18. Grafo de dependencias de una página (visualización)
```cypher
MATCH path = (page:AspxPage {name: 'Dashboard.aspx'})
             -[:CODE_BEHIND|HAS_CONTROL|TRIGGERS|HANDLES_EVENT*1..4]->()
RETURN path
LIMIT 50
```

### 19. Controles más comunes en el sistema
```cypher
MATCH (control:AspxControl)
RETURN control.type, count(*) as Count
ORDER BY Count DESC
LIMIT 10
```

### 20. Páginas que usan clases externas (no code-behind directo)
```cypher
MATCH (page:AspxPage)-[:CODE_BEHIND]->(cb:Class)
      -[:DEPENDS_ON|CALLS*1..2]->(external:Class)
WHERE NOT EXISTS((page)-[:CODE_BEHIND]->(external))
RETURN page.name, cb.name as CodeBehind, collect(DISTINCT external.name) as ExternalDeps
LIMIT 20
```

## 🔎 Búsqueda de Patrones Específicos

### 21. Páginas con GridViews (datos tabulares)
```cypher
MATCH (page:AspxPage)-[:HAS_CONTROL]->(grid:AspxControl)
WHERE grid.type =~ '(?i).*gridview.*'
RETURN page.name, grid.name, grid.events
```

### 22. Páginas con formularios de entrada (TextBox + Button)
```cypher
MATCH (page:AspxPage)-[:HAS_CONTROL]->(txt:AspxControl),
      (page)-[:HAS_CONTROL]->(btn:AspxControl)
WHERE txt.type =~ '(?i).*textbox.*'
  AND btn.type =~ '(?i).*button.*'
RETURN page.name, collect(txt.name) as TextBoxes, collect(btn.name) as Buttons
```

### 23. Detectar uso de ViewState (común en WebForms)
```cypher
MATCH (page:AspxPage)-[:CODE_BEHIND]->(class:Class)
      -[:HAS_METHOD]->(method:Method)
WHERE method.body =~ '(?i).*viewstate.*'
RETURN page.name, method.name, class.name
```

## 📈 Métricas de Complejidad

### 24. Complejidad por página (controles × eventos)
```cypher
MATCH (page:AspxPage)-[:HAS_CONTROL]->(control:AspxControl)
OPTIONAL MATCH (control)-[:TRIGGERS]->(event:AspxEvent)
RETURN page.name,
       count(DISTINCT control) as Controls,
       count(event) as Events,
       count(DISTINCT control) * count(event) as Complexity
ORDER BY Complexity DESC
```

### 25. Páginas sin controles interactivos
```cypher
MATCH (page:AspxPage)
WHERE NOT EXISTS((page)-[:HAS_CONTROL]->(:AspxControl)-[:TRIGGERS]->())
RETURN page.name, page.filePath
```

## 🛠️ Mantenimiento y Migración

### 26. Identificar páginas candidatas para migración a Razor
```cypher
MATCH (page:AspxPage)
OPTIONAL MATCH (page)-[:HAS_CONTROL]->(control:AspxControl)-[:TRIGGERS]->(event:AspxEvent)
WITH page, count(control) as controlCount, count(event) as eventCount
WHERE controlCount < 5 AND eventCount < 3
RETURN page.name, page.codeBehindClass, controlCount, eventCount
ORDER BY controlCount + eventCount
```

### 27. Buscar uso de controles obsoletos
```cypher
MATCH (control:AspxControl)
WHERE control.type IN ['asp:DataGrid', 'asp:DataList', 'asp:Repeater']
RETURN control.type, count(*) as Usage, collect(DISTINCT control.pageId) as Pages
```

### 28. Páginas con más de 50 líneas de inline code (anti-pattern)
```cypher
// Esta consulta requeriría análisis del body de la página
// Por ahora, podemos identificar páginas complejas por número de controles
MATCH (page:AspxPage)-[:HAS_CONTROL]->(control:AspxControl)
WITH page, count(control) as controlCount
WHERE controlCount > 20
RETURN page.name, page.filePath, controlCount
ORDER BY controlCount DESC
```

## 🎯 Uso con Vector Search

### 29. Buscar páginas similares semánticamente (requiere embeddings)
```cypher
// Ejemplo conceptual - requiere integración con vector index
MATCH (page:AspxPage)
WHERE page.name = 'UserProfile.aspx'
RETURN page.name, page.codeBehindClass

// Luego usar el vector index para búsqueda de similitud
```

### 30. Encontrar funcionalidad por descripción
```cypher
// Buscar en chunks generados por CodeChunker
// "aspx_page", "aspx_control", "aspx_event"
// Usar el VectorIndex con búsqueda semántica
```

## 💡 Tips de Uso

1. **Visualización en Neo4j Browser**: Usa `LIMIT` para evitar sobrecarga visual
2. **Performance**: Crea índices en propiedades frecuentemente consultadas:
   ```cypher
   CREATE INDEX aspx_page_name FOR (p:AspxPage) ON (p.name)
   CREATE INDEX aspx_control_type FOR (c:AspxControl) ON (c.type)
   ```
3. **Debugging**: Agrega `PROFILE` antes de la consulta para ver el plan de ejecución
4. **Export**: Usa `CALL apoc.export.*` para exportar resultados (requiere APOC plugin)

## 🔗 Recursos Adicionales

- [Neo4j Cypher Manual](https://neo4j.com/docs/cypher-manual/current/)
- [APOC Documentation](https://neo4j.com/labs/apoc/)
- [Graph Data Science](https://neo4j.com/product/graph-data-science/)
