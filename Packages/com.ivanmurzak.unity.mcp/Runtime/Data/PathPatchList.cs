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
    [Description("List of path-scoped patches routed through Reflector.TryModifyAt.")]
    public class PathPatchList : List<PathPatch>
    {
        public PathPatchList() { }

        public PathPatchList(int capacity) : base(capacity) { }

        public PathPatchList(IEnumerable<PathPatch> collection) : base(collection) { }

        public override string ToString()
        {
            if (Count == 0)
                return "No patches";

            var stringBuilder = new System.Text.StringBuilder();

            stringBuilder.AppendLine($"Patches total amount: {Count}");

            for (int i = 0; i < Count; i++)
                stringBuilder.AppendLine($"PathPatch[{i}] {this[i]}");

            return stringBuilder.ToString();
        }
    }
}
