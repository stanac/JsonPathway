# JsonPathway

[JsonPath](https://goessner.net/articles/JsonPath/) implementation in .NET standard 2.0 
that depends only on [System.Text.Json](https://www.nuget.org/packages/System.Text.Json/4.7.1).

## Suported operators

| JSONPath | Description |
| --- | --- |
| `$` | Root object, optional |
| `.` or `[]` | Child operator |
| `[]` | Array element operator |
| `[,]` | Multiple array elements |
| `[:]` `[::]` | Slice operator |
| `*` | Wildcard for properties |
| `[*]` | Wildcard for array elements (useless?) |
| `[?()]` | Filter for object properties or array elements |

`()` script expression is not supported in this implementation

## Usage

Install nuget `JsonPathway`

```csharp
using JsonPathway;
using System.Text.Json;
using System.Collections.Generic;

// ...
string jsonInput = LoadJson(); // or however you get your JSON string
string path = "$.store.bicycle.color.length"; // $ is optional
IReadOnlyList<JsonElement> result = JsonPath.ExecutePath(path, jsonInput);

// optionally to convert result to JSON use
string resultJson = JsonSerializer.Serialize(result);
```

Overrides:
```csharp
IReadOnlyList<JsonElement> ExecutePath(string jsonPathExpression, string json)
IReadOnlyList<JsonElement> ExecutePath(string jsonPathExpression, JsonDocument doc)
IReadOnlyList<JsonElement> ExecutePath(ExpressionList jsonPathExpression, string json)
IReadOnlyList<JsonElement> ExecutePath(ExpressionList jsonPathExpression, JsonDocument doc)
```

Both parsed document `JsonDocument` and `ExpressionList` that represents parsed path can be reused
and should be resued when used multiple times.
``` csharp
string json1 = // ...
string json2 = // ...
string json3 = // ...

string pathString = "$.store.bicycle.color.length";
ExpressionList expression = JsonPathExpression.Parse(pathString);
JsonDocument doc = JsonDocument.Parse(json1);

IReadOnlyList<JsonElement> result1 = JsonPath.ExecutePath(expression, doc);
IReadOnlyList<JsonElement> result2 = JsonPath.ExecutePath(expression, json2);
IReadOnlyList<JsonElement> result3 = JsonPath.ExecutePath(pathString, json3);
```

### Examples

For all examples following JSON will be used as input (taken from
[here](https://goessner.net/articles/JsonPath/)):
```json
{ 
  "store": {
    "book": [ 
      { "category": "reference",
        "author": "Nigel Rees",
        "title": "Sayings of the Century",
        "price": 8.95
      },
      { "category": "fiction",
        "author": "Evelyn Waugh",
        "title": "Sword of Honour",
        "price": 12.99
      },
      { "category": "fiction",
        "author": "Herman Melville",
        "title": "Moby Dick",
        "isbn": "0-553-21311-3",
        "price": 8.99
      },
      { "category": "fiction",
        "author": "J. R. R. Tolkien",
        "title": "The Lord of the Rings",
        "isbn": "0-395-19395-8",
        "price": 22.99
      }
    ],
    "bicycle": {
      "color": "red",
      "price": 19.95
    }
  }
}
```

## Auto property `length`

`length` is supported on both arrays and strings.

For path `$.store.bicycle.color.length` method `ExecutePath` returns JSON array `[3]`;

## Supported methods

#### Supported string methods

- `toUpper()`
- `toLower()`
- `toUpperCase() - alias of toUpper()`
- `toLowerCase() - alias of toLower()`
- `contains(value: string)`
- `contains(value: string, ignoreCase: boolean)`
- `startsWith(string value)`
- `startsWith(string value, ignoreCase: boolean)`
- `endsWith(string value)`
- `endsWith(string value, ignoreCase: boolean)`

#### Supported array methods
 - `contains(element: any)`

### Supported methods example

