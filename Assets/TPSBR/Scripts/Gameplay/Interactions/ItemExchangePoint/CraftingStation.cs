using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
        public abstract class CraftingStation : ItemExchangePoint
        {
                [Header("Recipes")]
                [SerializeField]
                private RecipeDefinition[] _recipes;

                public IReadOnlyList<RecipeDefinition> Recipes => _recipes ?? Array.Empty<RecipeDefinition>();
        }
}
