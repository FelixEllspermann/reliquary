using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Rouge.Tcg;

namespace Rouge.Tcg.EditorTools
{
    // Incarnates (29.08.2026): pro Archetyp werden ~2 niedrigstufige Monster zu
    // VESSELS — den Gefäßen der Incarnate-Opfergabe (mind. ein Opfer muss ein
    // Vessel sein). Deterministisch: je Familie die zwei Monster mit dem
    // niedrigsten Level (Gleichstand: niedrigste ATK, dann Name); der Lauf
    // resettet vorher ALLE isVessel-Flags und ist damit idempotent.
    public static partial class Batch2026Builder
    {
        [MenuItem("Rouge TCG/Mark Vessels (2 je Archetyp)")]
        public static void MarkVessels()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CardCatalog>(CatalogPath);
            var families = ArchetypeCatalog.Names
                .Concat(new[] { "Giftwyrm", "Splithoof", "Waylay", "Bylaw", "Chimekeep" })
                .Distinct().ToArray();

            int reset = 0;
            foreach (var card in catalog.cards)
                if (card is MonsterCardData monster && monster.isVessel)
                {
                    monster.isVessel = false;
                    EditorUtility.SetDirty(monster);
                    reset++;
                }

            var log = new System.Text.StringBuilder();
            int marked = 0;
            foreach (var family in families)
            {
                var members = catalog.cards
                    .OfType<MonsterCardData>()
                    .Where(m => !(m is ReliquaryCardData) && !(m is IncarnateCardData) && !m.isToken
                        && (m.cardName.StartsWith(family)
                            || (ArchetypeCatalog.Exceptions.TryGetValue(m.cardName, out var home) && home == family)))
                    .OrderBy(m => m.level).ThenBy(m => m.atk).ThenBy(m => m.cardName)
                    .Take(2).ToList();
                foreach (var vessel in members)
                {
                    vessel.isVessel = true;
                    EditorUtility.SetDirty(vessel);
                    marked++;
                    log.Append($"{vessel.cardName} (Lv{vessel.level}), ");
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Vessels] {marked} Vessels markiert ({reset} alte Flags geräumt): {log}");
        }
    }
}
