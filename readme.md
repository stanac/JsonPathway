# JsonPathway

[![.NET Core](https://github.com/stanac/JsonPathway/workflows/.NET%20Core/badge.svg)](https://github.com/stanac/JsonPathway/actions?query=workflow%3A%22.NET+Core%22)      [![Nuget](https://img.shields.io/nuget/vpre/jsonpathway)](https://www.nuget.org/packages/JsonPathway/)      [![Coverage Status](https://coveralls.io/repos/github/stanac/JsonPathway/badge.svg?branch=master)](https://coveralls.io/github/stanac/JsonPathway?branch=master)

---

[JsonPath](https://goessner.net/articles/JsonPath/) implementation in .NET 8.0+
that depends only on [System.Text.Json](https://www.nuget.org/packages/System.Text.Json/4.7.1).

## Changes

- 1.100.1 - fix nuget package
- 1.100.2 - fix bug #1
- 2.0.100 - Update System.Text.Json to 5.0.2 and update tests to use .NET 5
- 2.1.100 - Return clones of JsonElements when executing path so it's safe to dispose JsonDocument
- 2.1.101 - Fix filter expressions when comparing with `null` values
- 2.2.100 - Overloads to execute JsonPath on JsonElement and symbols package
- 2.3.100 - Support 5.x.x - 6.x.x `System.Text.Json` versions
- 2.4.100 - Support 5.x.x - 7.x.x `System.Text.Json` versions
- 2.5.100 - Support 5.x.x - 8.x.x `System.Text.Json` versions
- 3.0.100 - Drop support for `netstandard2.0`, support only version 8 and newer of `System.Text.Json`

## Supported operators

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
| `@` | Current element reference in filter |

`()` script expression is not supported in this implementation

## Usage

Install [nuget](https://www.nuget.org/packages/JsonPathway/) `JsonPathway`

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

Overloads:
```csharp
IReadOnlyList<JsonElement> ExecutePath(string jsonPathExpression, string json)
IReadOnlyList<JsonElement> ExecutePath(string jsonPathExpression, JsonDocument doc)
IReadOnlyList<JsonElement> ExecutePath(string jsonPathExpression, JsonElement element)
IReadOnlyList<JsonElement> ExecutePath(ExpressionList jsonPathExpression, string json)
IReadOnlyList<JsonElement> ExecutePath(ExpressionList jsonPathExpression, JsonDocument doc)
IReadOnlyList<JsonElement> ExecutePath(ExpressionList jsonPathExpression, JsonElement element)
```

Both parsed document `JsonDocument` and `ExpressionList` that represents parsed path can be reused
and should be reused when used multiple times.
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

Validating input can be done with:
```csharp
bool valid = JsonPath.IsPathValid(path, out string error);
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

For path `$.store.book[?(@.title.length == 21)]` resulting JSON array is:
```json
[
  { "category": "fiction",
    "author": "J. R. R. Tolkien",
    "title": "The Lord of the Rings",
    "isbn": "0-395-19395-8",
    "price": 22.99
  }
]
```

## Supported methods

**Methods are supported only in filters**

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

##### Supported methods example

Path `$.store.book[?(@.author.contains("tolkien", true))]` returns

```json
[
  {
    "category": "fiction",
    "author": "J. R. R. Tolkien",
    "title": "The Lord of the Rings",
    "isbn": "0-395-19395-8",
    "price": 22.99
  }
]
```

Same goes for path `$.store.book[?(@.author.contains('tolkien', true))]` even though single 
quotes are not supported by "specification" they are supported by this implementation for string quotes.

##### Other examples

Following child operators all return same result (`[19.95]`):
 - `$.store.bicycle.price`
 - `store.bicycle.price`
 - `$["store"]["bicycle"]["price"]`
 - `["store"]["bicycle"]["price"]`
 - `$['store']['bicycle']['price']`
 - `['store']['bicycle']['price']`

Which means `$` is optional and strings can be quoted with `'` and `"`.

Array operators:
 - `$.store.book[0]` returns "Sayings of the Century" book
 - `$.store.book[-1]` returns "The Lord of the Rings" book (last book)
 - `$.store.book[*]` returns all books
 
Slice operator `[start:end:step]` `$.store.book[0:4:2]` returns books at indexes [0] and [2]
(second number "end" is exclusive).

Wildcard can be applied to object properties with `.*`:
- `$.store.bicycle.*` returns `["red",19.95]`

Recursive operator can be applied to properties and arrays with `..` e.g.

`$.store.book..` results in
```json
[
  [
    {
      "category": "reference",
      "author": "Nigel Rees",
      "title": "Sayings of the Century",
      "price": 8.95
    },
    {
      "category": "fiction",
      "author": "Evelyn Waugh",
      "title": "Sword of Honour",
      "price": 12.99
    },
    {
      "category": "fiction",
      "author": "Herman Melville",
      "title": "Moby Dick",
      "isbn": "0-553-21311-3",
      "price": 8.99
    },
    {
      "category": "fiction",
     "author": "J. R. R. Tolkien",
     "title": "The Lord of the Rings",
     "isbn": "0-395-19395-8",
     "price": 22.99
    }
  ],
  {
  	"category": "reference",
  	"author": "Nigel Rees",
  	"title": "Sayings of the Century",
  	"price": 8.95
  },
  {
  	"category": "fiction",
  	"author": "Evelyn Waugh",
  	"title": "Sword of Honour",
  	"price": 12.99
  },
  {
  	"category": "fiction",
  	"author": "Herman Melville",
  	"title": "Moby Dick",
  	"isbn": "0-553-21311-3",
  	"price": 8.99
  },
  {
    "category": "fiction",
    "author": "J. R. R. Tolkien",
    "title": "The Lord of the Rings",
    "isbn": "0-395-19395-8",
    "price": 22.99
  }
]
```

Filters can be applied to array elements and object property values:
 
- `$.store.book[?(@.price > 10)]` returns:
```json
[
  {
    "category": "fiction",
    "author": "Evelyn Waugh",
    "title": "Sword of Honour",
    "price": 12.99
  },
  {
    "category": "fiction",
    "author": "J. R. R. Tolkien",
    "title": "The Lord of the Rings",
    "isbn": "0-395-19395-8",
    "price": 22.99
  }
]
```

- `$.store.book[?(@.isbn)]` (truthy filter) returns:
```json
[
  {
    "category": "fiction",
    "author": "Herman Melville",
    "title": "Moby Dick",
    "isbn": "0-553-21311-3",
    "price": 8.99
  },
  {
    "category": "fiction",
    "author": "J. R. R. Tolkien",
    "title": "The Lord of the Rings",
    "isbn": "0-395-19395-8",
    "price": 22.99
  }
]
```
