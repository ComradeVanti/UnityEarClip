# Ear-clip 

[![openupm](https://img.shields.io/npm/v/dev.comradevanti.ear-clip?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/dev.comradevanti.ear-clip/)

Super lightweight ear-clipping 2D polygon triangulation package for Unity.
Can only handle [simple](https://en.wikipedia.org/wiki/Simple_polygon) polygons.

[Changelog](./CHANGELOG.md)

## Usage

```csharp
using Dev.ComradeVanti.EarClip;

var points = new Vector2[] { ... };

// Points need to be in clockwise order
// Sort if needed
points = points.Clockwise().ToArray();

// Triangulate
var triangles = Triangulate.ConcaveNoHoles(points).ToArray();
```
## Installation

Install via OpenUPM `openupm add dev.comradevanti.ear-clip`

## Compatibility

Developed with for Unity 2021.3.