/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/

#nullable enable
using System.Collections.Generic;
using System.ComponentModel;

namespace AIGD
{
    [Description("Per-GameObject list of path-scoped patch lists. " +
        "Outer index aligns with 'gameObjectRefs'; inner list contains {path, value} entries.")]
    public class PathPatchesPerGameObjectList : List<PathPatchList?>
    {
        public PathPatchesPerGameObjectList() { }

        public PathPatchesPerGameObjectList(int capacity) : base(capacity) { }

        public PathPatchesPerGameObjectList(IEnumerable<PathPatchList?> collection) : base(collection) { }
    }
}
