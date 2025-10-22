using System;
using System.Collections.Generic;
using UnityEngine;

namespace TPSBR
{
    public abstract class CraftingStation : ItemExchangePoint
    {
        public IReadOnlyList<RecipeDefinition> Recipes => _recipes ?? Array.Empty<RecipeDefinition>();
        [Header("Recipes")]
        [SerializeField]
        private RecipeDefinition[] _recipes;
 }
}
