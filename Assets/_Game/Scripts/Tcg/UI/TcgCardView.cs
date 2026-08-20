using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rouge.Tcg.UI
{
    /// <summary>
    /// Kartenansicht im "Reliquary"-Design (design_handoff_tcg_card_system):
    /// Die Karte ist intern fix 480x672 (CardRoot) und wird uniform auf die
    /// vom Layout vorgegebene Root-Größe skaliert. Klick-, Hover- und Drag-API
    /// ist unverändert zur alten Ansicht.
    /// </summary>
    public class TcgCardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>Standard-Farbe der Outline (gelb = Ziel/Aktion).</summary>
        public static readonly Color TargetHighlight = new Color(1f, 0.90f, 0.30f, 0.40f);
        /// <summary>Grünliche Outline für "diese Karte ist gerade spielbar/aktivierbar".</summary>
        public static readonly Color PlayableHighlight = new Color(0.35f, 0.95f, 0.55f, 0.42f);

        // Design-Referenzgröße (Screen scale des Handoffs)
        private const float DesignW = 480f;
        private const float DesignH = 672f;

        [Header("Design (im Prefab verdrahtet)")]
        [SerializeField] private CardSkin skin;
        [SerializeField] private RectTransform cardRoot;
        [SerializeField] private GameObject front;
        [SerializeField] private Image chassisImage;
        [SerializeField] private Image artworkImage;
        [SerializeField] private Image vignetteImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Image crestImage;
        [SerializeField] private TMP_Text crestText;
        [SerializeField] private RectTransform badgeRect;
        [SerializeField] private Image badgeImage;
        [SerializeField] private TMP_Text badgeText;
        [SerializeField] private RectTransform stripRect;
        [SerializeField] private Image stripBorder;
        [SerializeField] private RectTransform pipRect;
        [SerializeField] private Image pipImage;
        [SerializeField] private TMP_Text attributeText;
        [SerializeField] private TMP_Text typeText;
        [SerializeField] private RectTransform effectRect;
        [SerializeField] private TMP_Text effectText;
        [SerializeField] private GameObject statsRoot;
        [SerializeField] private TMP_Text dmgLabel;
        [SerializeField] private TMP_Text dmgValue;
        [SerializeField] private TMP_Text defLabel;
        [SerializeField] private TMP_Text defValue;
        [SerializeField] private Image backOverlay;
        [SerializeField] private GameObject highlightFrame;

        [Header("Kompakt-Rendition (Feld/Hand, 112x157)")]
        [SerializeField] private RectTransform compactRoot;
        [SerializeField] private Image cChassis;
        [SerializeField] private Image cArt;
        [SerializeField] private TMP_Text cName;
        [SerializeField] private GameObject cMeta;
        [SerializeField] private Image cPip;
        [SerializeField] private TMP_Text cAttr;
        [SerializeField] private TMP_Text cType;
        [SerializeField] private GameObject cStats;
        [SerializeField] private TMP_Text cAtk;
        [SerializeField] private TMP_Text cDef;
        [SerializeField] private TMP_Text cFooter;
        [SerializeField] private Image cCrest;
        [SerializeField] private TMP_Text cCrestText;
        [SerializeField] private Image cBack;
        [SerializeField] private GameObject cHighlight;

        /// <summary>Unterhalb dieser Root-Breite wird die Kompakt-Rendition gezeigt.</summary>
        private const float CompactThreshold = 200f;
        private const float CompactW = 112f;
        private const float CompactH = 157f;

        [Header("Drag & Drop")]
        [Range(1f, 1.5f)]
        [Tooltip("Vergrößerung der Karte, während sie gezogen wird")]
        [SerializeField] private float dragScale = 1.12f;

        public CardInstance Instance { get; private set; }
        public bool HiddenFace { get; private set; }

        public event Action<TcgCardView> Clicked;
        public event Action<TcgCardView> Hovered;
        public event Action<TcgCardView> Unhovered;
        public event Action<TcgCardView> DragStarted;
        public event Action<TcgCardView, Vector2> DragEnded;

        private Image highlightImage;
        private bool dragging;
        private Vector3 preDragScale;

        /// <summary>Vom Renderer nur für eigene Handkarten gesetzt: Hover hebt die Karte leicht an.</summary>
        public bool HoverLift;
        private Coroutine liftRoutine;
        private Vector2 liftBasePos;
        private bool liftBaseCaptured;
        private bool lifted;

        // ---------- Design-Tokens (Ink-Farben je Kartentyp) ----------
        private struct Inks
        {
            public Color name, crest, badge, metaStrong, metaMuted, keyline;
            public Color statLabelStrong, statLabelMuted, statInkStrong, statInkMuted, statInkDisabled;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }

        /// <summary>
        /// Der Kartenrücken des Besitzers, oder null für den Standard. Der Gegner
        /// bekommt seinen eigenen — sein Fach kommt mit der Match-Nachricht, also
        /// vor dem ersten Bild des Duells. Kennt dieser Client den Gegenstand
        /// nicht, fällt er still auf Vanilla zurück.
        /// </summary>
        private static Sprite EquippedBack(CardInstance instance)
        {
            bool mine = instance == null || instance.Owner == null || instance.Owner.IsLocal;
            return mine
                ? Rouge.Tcg.Net.CosmeticArt.EquippedCardBack()
                : Rouge.Tcg.Net.CosmeticArt.CardBack(Rouge.Tcg.Net.MatchContext.RemoteEquipped("sleeve"));
        }

        private static readonly Inks MonsterInks = new Inks
        {
            name = Hex("#F1DFB8"), crest = Hex("#F3DDA4"), badge = Hex("#1E1405"),
            metaStrong = Hex("#E4D3AE"), metaMuted = Hex("#B5A484"), keyline = Hex("#C8A45C"),
            statLabelStrong = Hex("#B79A62"), statLabelMuted = Hex("#8D8570"),
            statInkStrong = Hex("#F3DDA4"), statInkMuted = Hex("#DCD3BC"), statInkDisabled = Hex("#4A4360")
        };

        private static readonly Inks SpellInks = new Inks
        {
            name = Hex("#DCF0F4"), crest = Hex("#B9E6F0"), badge = Hex("#04191D"),
            metaStrong = Hex("#CDE6EB"), metaMuted = Hex("#89A8B0"), keyline = Hex("#8FC6D2")
        };

        private static readonly Inks ArtifactInks = new Inks
        {
            name = Hex("#E9E0F8"), crest = Hex("#D8CAF6"), badge = Hex("#100A1E"),
            metaStrong = Hex("#DDD3F0"), metaMuted = Hex("#9A8FB8"), keyline = Hex("#B9A3E0"),
            statLabelStrong = Hex("#9A8AC4"), statLabelMuted = Hex("#6D6684"),
            statInkStrong = Hex("#D8CAF6"), statInkMuted = Hex("#D8CAF6"), statInkDisabled = Hex("#4A4360")
        };

        /// <summary>Reliquary: dunkle Tinte auf hellem Ivory-Chassis, Gold-Keyline.</summary>
        private static readonly Inks ReliquaryInks = new Inks
        {
            name = Hex("#3A2F1B"), crest = Hex("#4A3B20"), badge = Hex("#F7F1E1"),
            metaStrong = Hex("#5A4A2C"), metaMuted = Hex("#8A7B5C"), keyline = Hex("#C8A45C"),
            statLabelStrong = Hex("#8A7343"), statLabelMuted = Hex("#9A9078"),
            statInkStrong = Hex("#4A3B20"), statInkMuted = Hex("#5C5138"), statInkDisabled = Hex("#B5AC97")
        };

        private static readonly Color EffectInk = Hex("#2E2417");

        /// <summary>Pip-Farben aus dem Handoff ("Attribute pips").</summary>
        public static Color AttributePipColor(MonsterAttribute attribute)
        {
            switch (attribute)
            {
                case MonsterAttribute.Fire: return Hex("#E0603A");
                case MonsterAttribute.Water: return Hex("#4B92D6");
                case MonsterAttribute.Light: return Hex("#E8D08A");
                case MonsterAttribute.Dark: return Hex("#8B6BC4");
                case MonsterAttribute.Earth: return Hex("#A8894F");
                default: return Hex("#6FBF9A"); // Wind
            }
        }

        private void OnRectTransformDimensionsChange() => FitCardRoot();

        private bool UseCompact()
        {
            if (compactRoot == null) return false;
            return ((RectTransform)transform).rect.width < CompactThreshold;
        }

        /// <summary>Skaliert die fixe Design-Geometrie (voll oder kompakt) uniform in die Root-Größe.</summary>
        private void FitCardRoot()
        {
            var rect = ((RectTransform)transform).rect;
            if (rect.width <= 0f || rect.height <= 0f) return;
            if (cardRoot != null)
            {
                float scale = Mathf.Min(rect.width / DesignW, rect.height / DesignH);
                cardRoot.localScale = new Vector3(scale, scale, 1f);
            }
            if (compactRoot != null)
            {
                float scale = Mathf.Min(rect.width / CompactW, rect.height / CompactH);
                compactRoot.localScale = new Vector3(scale, scale, 1f);
            }
        }

        public void Show(CardInstance instance, bool hideFace, bool upright = false, bool revealFaceDown = false)
        {
            Instance = instance;
            // revealFaceDown: die Inspect-Ansicht darf eigene verdeckte Karten
            // aufgedeckt zeigen — was man nicht sehen darf, kommt gar nicht erst
            // hier an (ShowHiddenCard-Weiche bzw. Definition == null im Mirror).
            HiddenFace = hideFace || (!revealFaceDown && instance != null && instance.FaceDown);
            SetHighlight(false);

            if (instance == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            FitCardRoot();

            // Spiegel-Platzhalter aus Server-Duellen haben keine Definition —
            // der Server verrät sie nicht. Solche Karten sind immer Rückseiten.
            bool showBack = HiddenFace || instance.Definition == null;
            var rect = (RectTransform)transform;
            bool defense = !upright && instance.Zone == ZoneType.MonsterZone && instance.Position == BattlePosition.Defense;
            rect.localEulerAngles = defense ? new Vector3(0, 0, -90f) : Vector3.zero;

            bool compact = UseCompact();
            // Liegende Karten leicht verkleinern (157 breit > 112er-Zone), damit sie Nachbarn nicht überdecken
            rect.localScale = defense && compact ? new Vector3(0.78f, 0.78f, 1f) : Vector3.one;
            if (cardRoot != null) cardRoot.gameObject.SetActive(!compact);
            if (compactRoot != null) compactRoot.gameObject.SetActive(compact);

            // Das Finish liegt auf der Karte selbst, nicht auf dem Layout-Platz —
            // deshalb an CardRoot und nicht an die Wurzel. Mit der Rückseite
            // verschwindet es: eine funkelnde Rückseite verriete, was dort liegt.
            //
            // Hier steht der einzige Ort, an dem im Spiel Finishes gezeichnet
            // werden. Jede Ansicht, die eine Karte zeigt — Hand, Feld, Friedhof,
            // Vorschau, Pack-Reveal — bekommt sie damit von selbst.
            var finishHost = compact ? compactRoot : cardRoot;
            if (finishHost != null)
                CardFinishOverlay.Apply(finishHost, showBack ? Net.CardFinish.Plain : instance.Finish);

            // Status-Badges (NEG / Death Counter) kleben auf der Karte selbst —
            // verdeckte Karten zeigen nichts, sonst wäre die Info verraten.
            UpdateStatusBadges(finishHost, instance, showBack);

            if (compact)
            {
                ShowCompact(instance, showBack);
                return;
            }

            if (backOverlay != null)
            {
                backOverlay.sprite = EquippedBack(instance) ?? (skin != null ? skin.cardBack : null);
                backOverlay.gameObject.SetActive(showBack);
            }
            if (front != null) front.SetActive(!showBack);

            if (showBack || skin == null) return;

            var definition = instance.Definition;
            var monster = definition as MonsterCardData;
            var spell = definition as SpellCardData;
            var artifact = definition as ArtifactCardData;
            var playerCard = definition as PlayerCardData;
            bool isReliquary = definition is ReliquaryCardData;

            Inks inks = isReliquary ? ReliquaryInks
                : monster != null || playerCard != null ? MonsterInks
                : spell != null ? SpellInks : ArtifactInks;

            // Chassis + Badge + Crest
            if (chassisImage != null) chassisImage.sprite = skin.ChassisFor(definition);
            if (badgeImage != null)
                badgeImage.sprite = isReliquary && skin.badgeReliquary != null ? skin.badgeReliquary
                    : monster != null || playerCard != null ? skin.badgeMonster
                    : spell != null ? skin.badgeSpell : skin.badgeArtifact;

            // Name (Cinzel, nameInk, einzeilig)
            if (nameText != null)
            {
                nameText.text = Loc.CardName(definition.cardName);
                nameText.color = inks.name;
            }

            // Level-Wappen: nur Monster (Reliquarys tragen ein "R" statt Level)
            bool hasCrest = monster != null;
            if (crestImage != null)
            {
                crestImage.gameObject.SetActive(hasCrest);
                if (hasCrest)
                {
                    crestImage.sprite = skin.crestMonster;
                    if (crestText != null)
                    {
                        crestText.text = isReliquary ? "R" : Mathf.Clamp(monster.level, 1, 3).ToString();
                        crestText.color = inks.crest;
                    }
                }
            }

            // Badge-Text + dynamische Breite, Meta-Strip füllt den Rest
            string badge = Loc.T(isReliquary ? "RELIQUARY" : monster != null ? "MONSTER" : spell != null ? "SPELL" : artifact != null ? "ARTIFACT" : "PLAYER");
            if (badgeText != null)
            {
                badgeText.text = badge;
                badgeText.color = inks.badge;
            }
            LayoutBadgeRow();

            // Meta-Strip: Pip + links/rechts
            string left, right;
            bool showPip = monster != null;
            if (monster != null)
            {
                left = Loc.T(monster.attribute.ToString().ToUpperInvariant());
                right = Loc.T(monster.monsterType.ToString().ToUpperInvariant());
                if (pipImage != null) pipImage.color = AttributePipColor(monster.attribute);
            }
            else if (spell != null)
            {
                left = Loc.T("SPELL");
                right = Loc.T(spell.speed == SpellSpeed.Quick ? "QUICK" : "NORMAL");
            }
            else if (artifact != null)
            {
                left = Loc.T("ARTIFACT");
                right = Loc.T(ArtifactSlotName(artifact.slot).ToUpperInvariant());
            }
            else
            {
                left = Loc.T("HERO");
                right = playerCard != null ? $"{playerCard.startLifePoints} LP" : "";
            }
            if (pipRect != null) pipRect.gameObject.SetActive(showPip);
            if (attributeText != null)
            {
                attributeText.text = left;
                attributeText.color = inks.metaStrong;
                var attrRect = (RectTransform)attributeText.transform;
                attrRect.offsetMin = new Vector2(showPip ? 27f : 11f, attrRect.offsetMin.y);
            }
            if (typeText != null)
            {
                typeText.text = right;
                typeText.color = inks.metaMuted;
            }
            if (stripBorder != null)
            {
                var c = inks.keyline;
                stripBorder.color = new Color(c.r, c.g, c.b, 0.45f);
            }

            // Effekt-Panel: Höhe je nach Stat-Row (128 bzw. 188)
            bool hasStats = monster != null || artifact != null;
            if (effectRect != null)
            {
                float height = hasStats ? 128f : 188f;
                float padX = hasStats ? 12f : 13f;
                float padY = hasStats ? 9f : 11f;
                effectRect.anchoredPosition = new Vector2(39f + padX, -(470f + padY));
                effectRect.sizeDelta = new Vector2(402f - 2f * padX, height - 2f * padY);
            }
            if (effectText != null)
            {
                effectText.text = BuildEffectBody(definition);
                effectText.color = EffectInk;
            }

            // Stat-Reihe (ATK/DEF)
            if (statsRoot != null) statsRoot.SetActive(hasStats);
            if (hasStats)
            {
                if (dmgLabel != null) dmgLabel.color = inks.statLabelStrong;
                if (defLabel != null) defLabel.color = inks.statLabelMuted;
                if (monster != null)
                {
                    if (dmgValue != null) dmgValue.text = ColorizeStat(instance.CurrentAtk, monster.atk, inks.statInkStrong);
                    if (defValue != null) defValue.text = ColorizeStat(instance.CurrentDef, monster.def, inks.statInkMuted);
                }
                else if (artifact != null)
                {
                    if (dmgValue != null)
                        dmgValue.text = artifact.atkBonus != 0
                            ? Colored($"+{artifact.atkBonus}", inks.statInkStrong)
                            : Colored("—", inks.statInkDisabled);
                    if (defValue != null)
                        defValue.text = artifact.defBonus != 0
                            ? Colored($"+{artifact.defBonus}", inks.statInkMuted)
                            : Colored("—", inks.statInkDisabled);
                }
            }

            // Artwork (1:1, cover) + Monster-Vignette
            if (artworkImage != null)
            {
                artworkImage.enabled = definition.artwork != null;
                artworkImage.sprite = definition.artwork;
            }
            if (vignetteImage != null)
            {
                vignetteImage.sprite = skin.artworkVignette;
                vignetteImage.enabled = monster != null && definition.artwork != null;
            }
        }

        /// <summary>Kompakte Feld-/Hand-Rendition (Handoff "Reduced card renditions").</summary>
        // ---- Status-Badges: Negated-Chip + Death-Counter (Totenkopf + Zahl) ----
        // Komplett zur Laufzeit gebaut (kein Prefab-/Szenen-Edit): ein kleiner
        // Streifen oben links auf der Karte. ASCII-Texte, damit kein Font-Atlas
        // leere Glyphen liefert; der Totenkopf ist ein prozedurales Mini-Sprite.
        private RectTransform statusBadgeRoot;
        private Image negChip;
        private Image dcChip;
        private TMP_Text dcText;
        private Image lienChip;     // The Small Print: Pfandrecht mit Betrag
        private TMP_Text lienText;
        private static Sprite cachedSkullSprite;

        private void UpdateStatusBadges(RectTransform host, CardInstance instance, bool showBack)
        {
            bool negated = !showBack && instance != null && instance.EffectsNegated;
            int counters = showBack || instance == null ? 0 : instance.DeathCounters;
            int lien = showBack || instance == null ? 0 : instance.LienAmount;
            if (!negated && counters <= 0 && lien <= 0)
            {
                if (statusBadgeRoot != null) statusBadgeRoot.gameObject.SetActive(false);
                return;
            }

            EnsureStatusBadges();
            if (host != null && statusBadgeRoot.parent != host)
                statusBadgeRoot.SetParent(host, false);
            statusBadgeRoot.SetAsLastSibling();
            statusBadgeRoot.gameObject.SetActive(true);

            negChip.gameObject.SetActive(negated);
            dcChip.gameObject.SetActive(counters > 0);
            if (counters > 0) dcText.text = counters.ToString();
            if (lienChip != null)
            {
                lienChip.gameObject.SetActive(lien > 0);
                if (lien > 0 && lienText != null) lienText.text = Loc.T("LIEN") + " " + lien;
            }

            float x = 0f;
            if (negated) { ((RectTransform)negChip.transform).anchoredPosition = new Vector2(x, 0f); x += 42f; }
            if (counters > 0) { ((RectTransform)dcChip.transform).anchoredPosition = new Vector2(x, 0f); x += 38f; }
            if (lien > 0 && lienChip != null) ((RectTransform)lienChip.transform).anchoredPosition = new Vector2(x, 0f);
        }

        private void EnsureStatusBadges()
        {
            if (statusBadgeRoot != null) return;

            var rootGo = new GameObject("StatusBadges", typeof(RectTransform));
            statusBadgeRoot = (RectTransform)rootGo.transform;
            statusBadgeRoot.anchorMin = statusBadgeRoot.anchorMax = new Vector2(0f, 1f);
            statusBadgeRoot.pivot = new Vector2(0f, 1f);
            statusBadgeRoot.anchoredPosition = new Vector2(4f, -4f);
            statusBadgeRoot.sizeDelta = new Vector2(10f, 18f);

            negChip = BuildChip(statusBadgeRoot, "NegChip", new Color(0.42f, 0.26f, 0.58f, 0.95f), 40f, out var negLabel);
            negLabel.text = "NEG";

            dcChip = BuildChip(statusBadgeRoot, "DeathChip", new Color(0.55f, 0.12f, 0.12f, 0.95f), 36f, out dcText);
            dcText.text = "0";
            ((RectTransform)dcText.transform).offsetMin = new Vector2(16f, 0f);

            var skullGo = new GameObject("Skull", typeof(RectTransform), typeof(Image));
            var skullRect = (RectTransform)skullGo.transform;
            skullRect.SetParent(dcChip.transform, false);
            skullRect.anchorMin = skullRect.anchorMax = new Vector2(0f, 0.5f);
            skullRect.pivot = new Vector2(0f, 0.5f);
            skullRect.anchoredPosition = new Vector2(2f, 0f);
            skullRect.sizeDelta = new Vector2(13f, 13f);
            var skullImage = skullGo.GetComponent<Image>();
            skullImage.sprite = SkullSprite();
            skullImage.raycastTarget = false;

            // The Small Print: Pfandrecht — Betrag in Gold auf dunklem Grund
            lienChip = BuildChip(statusBadgeRoot, "LienChip", new Color(0.55f, 0.42f, 0.14f, 0.95f), 52f, out lienText);
            lienText.text = "LIEN 1";
        }

        private Image BuildChip(RectTransform parent, string name, Color color, float width, out TMP_Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, 16f);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;

            var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            var textRect = (RectTransform)textGo.transform;
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            label = textGo.GetComponent<TextMeshProUGUI>();
            if (nameText != null) label.font = nameText.font;
            label.fontSize = 10f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;
            label.enableWordWrapping = false;
            return image;
        }

        /// <summary>12×12-Totenkopf, einmal pro Prozess gezeichnet (Zeile 0 = unten).</summary>
        private static Sprite SkullSprite()
        {
            if (cachedSkullSprite != null) return cachedSkullSprite;
            string[] rows =
            {
                "............",
                "............",
                "..X.XX.XX.X.",
                "..XXXXXXXX..",
                "..XXXXXXXX..",
                ".XXXXooXXXX.",
                ".XooXXXXooX.",
                ".XooXXXXooX.",
                ".XXXXXXXXXX.",
                ".XXXXXXXXXX.",
                "..XXXXXXXX..",
                "....XXXX....",
            };
            var tex = new Texture2D(12, 12, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 12; y++)
                for (int x = 0; x < 12; x++)
                {
                    char c = rows[y][x];
                    tex.SetPixel(x, y, c == 'X' ? Color.white
                        : c == 'o' ? new Color(0f, 0f, 0f, 0.85f)
                        : Color.clear);
                }
            tex.Apply();
            cachedSkullSprite = Sprite.Create(tex, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f), 12f);
            return cachedSkullSprite;
        }

        private void ShowCompact(CardInstance instance, bool showBack)
        {
            if (skin == null) return;
            var definition = instance.Definition;
            var monster = definition as MonsterCardData;
            var spell = definition as SpellCardData;
            var artifact = definition as ArtifactCardData;
            var playerCard = definition as PlayerCardData;
            bool isReliquary = definition is ReliquaryCardData;
            Inks inks = isReliquary ? ReliquaryInks
                : monster != null || playerCard != null ? MonsterInks
                : spell != null ? SpellInks : ArtifactInks;

            if (cBack != null)
            {
                var rootRect = ((RectTransform)transform).rect;
                // Rücken-Auflösung nach Größe: Gegnerhand < Zonen < große Reveal-Karten.
                // Ein ausgerüsteter Kartenrücken hat nur eine Fassung und ersetzt alle drei.
                cBack.sprite = EquippedBack(instance)
                    ?? (rootRect.width < 90f ? skin.backHand
                        : rootRect.width < 150f ? skin.backZone
                        : skin.backLogin);
                cBack.gameObject.SetActive(showBack);
            }
            if (cCrest != null) cCrest.gameObject.SetActive(!showBack && monster != null);
            if (showBack) return;

            if (cChassis != null) cChassis.sprite = skin.CompactChassisFor(definition);
            if (cName != null)
            {
                cName.text = Loc.CardName(definition.cardName);
                cName.color = inks.name;
            }
            if (cArt != null)
            {
                cArt.enabled = definition.artwork != null;
                cArt.sprite = definition.artwork;
            }

            bool isMonster = monster != null;
            if (cMeta != null) cMeta.SetActive(isMonster);
            if (cStats != null) cStats.SetActive(isMonster);
            if (cFooter != null) cFooter.gameObject.SetActive(!isMonster);

            if (isMonster)
            {
                if (cPip != null) cPip.color = AttributePipColor(monster.attribute);
                if (cAttr != null)
                {
                    cAttr.text = Loc.T(monster.attribute.ToString().ToUpperInvariant());
                    cAttr.color = inks.metaStrong;
                }
                if (cType != null)
                {
                    cType.text = Loc.T(monster.monsterType.ToString().ToUpperInvariant());
                    cType.color = inks.metaMuted;
                }
                if (cAtk != null) cAtk.text = ColorizeStat(instance.CurrentAtk, monster.atk, inks.statInkStrong);
                if (cDef != null) cDef.text = ColorizeStat(instance.CurrentDef, monster.def, inks.statInkMuted);
                if (cCrestText != null)
                {
                    cCrestText.text = isReliquary ? "R" : Mathf.Clamp(monster.level, 1, 3).ToString();
                    cCrestText.color = inks.crest;
                }
            }
            else if (cFooter != null)
            {
                string footer = spell != null
                    ? Loc.T(spell.speed == SpellSpeed.Quick ? "QUICK SPELL" : "SPELL")
                    : artifact != null
                        ? $"{Loc.T("ARTIFACT")} · {Loc.T(ArtifactSlotName(artifact.slot).ToUpperInvariant())}"
                        : Loc.T("HERO");
                cFooter.text = footer;
            }
        }

        /// <summary>Badge misst sich am Text; der Meta-Strip füllt die restliche Zeilenbreite.</summary>
        private void LayoutBadgeRow()
        {
            if (badgeRect == null || stripRect == null || badgeText == null) return;
            badgeText.ForceMeshUpdate();
            float badgeWidth = Mathf.Ceil(badgeText.GetPreferredValues(badgeText.text).x) + 22f; // padding 0 11
            badgeRect.anchoredPosition = new Vector2(39f, -437f);
            badgeRect.sizeDelta = new Vector2(badgeWidth, 29f);
            float stripX = 39f + badgeWidth + 4f;
            stripRect.anchoredPosition = new Vector2(stripX, -437f);
            stripRect.sizeDelta = new Vector2(441f - stripX, 29f);
        }

        private static string Colored(string text, Color color) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";

        /// <summary>Effekt-Fließtext fürs Pergament-Panel (Spectral): Summon-/Passiv-Zeilen + Effektliste.</summary>
        private static string BuildEffectBody(CardDefinition definition)
        {
            string prefix = "";
            if (definition is ReliquaryCardData reliquaryData)
            {
                if (!string.IsNullOrWhiteSpace(reliquaryData.summonText))
                    prefix = $"<b>{Loc.T("SUMMON:")}</b> {Loc.CardSummon(definition.cardName, reliquaryData.summonText)}";
            }
            else if (definition is MonsterCardData monsterData)
            {
                string condition = monsterData.SelfSummonConditionText();
                if (!string.IsNullOrEmpty(condition)) prefix = $"<b>{Loc.T("SUMMON:")}</b> {condition}";
            }
            // Dauerhafte Passiv-Fähigkeiten (Aura, Spott, Kampf-Schild, Rabatt ...)
            var passives = definition.BuildPassiveLines();
            if (passives.Count > 0)
            {
                string block = $"<b>{Loc.T("PASSIVE:")}</b> " + string.Join(" ", passives);
                prefix = prefix.Length > 0 ? prefix + "\n" + block : block;
            }

            string list = definition.effects == null || definition.effects.Count == 0 ? "" : BuildEffectList(definition);
            if (prefix.Length > 0 && list.Length > 0) return prefix + "\n" + list;
            return prefix.Length > 0 ? prefix : list;
        }

        private static string BuildEffectList(CardDefinition definition)
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < definition.effects.Count; i++)
            {
                var effect = definition.effects[i];
                if (effect == null || string.IsNullOrWhiteSpace(effect.text)) continue;
                if (sb.Length > 0) sb.Append('\n');
                string head;
                // Manakosten in Mana-Blau: auf der Karte selbst ist der Platz knapp,
                // die Farbe reicht — das volle Badge trägt die Inspect-Ansicht.
                // Dunkler Ton, weil das Textfeld der Karte hell ist.
                string costPart = effect.manaCost > 0 ? $" <color=#155A8A>{Loc.F("{0} MANA", effect.manaCost)}</color>" : "";
                if (effect.isInfused)
                {
                    // Coupled = Entweder-oder-Upgrade des Normal-Effekts → "OR INFUSED"
                    head = effect.infusedKind == InfusedKind.Coupled
                        ? $"{Loc.T("OR INFUSED")}{costPart}:"
                        : $"{Loc.T("INFUSED")}{costPart}:";
                }
                else head = $"{Loc.T("NORMAL")}{costPart}:";
                sb.Append("<b>").Append(head).Append("</b> ").Append(Loc.CardText(definition.cardName, i, effect.text));
            }
            return sb.ToString();
        }

        public static string ArtifactSlotName(ArtifactSlot slot)
        {
            switch (slot)
            {
                case ArtifactSlot.Monster: return "Monster";
                case ArtifactSlot.Player: return "Player";
                default: return "Field";
            }
        }

        /// <summary>Färbt veränderte Werte: grün = gebufft, rot = geschwächt, sonst Design-Tinte.</summary>
        private static string ColorizeStat(int current, int baseline, Color normalInk)
        {
            if (current > baseline) return $"<color=#7DDB6E>{current}</color>";
            if (current < baseline) return $"<color=#E8695E>{current}</color>";
            return Colored(current.ToString(), normalInk);
        }

        // ---- Aktivierungs-Ladung (Handoff „Animations", Abschnitt 3) ----

        private static readonly Color ChargeInk = new Color(0.973f, 0.933f, 0.839f);   // #F8EED6
        private static readonly Color ChargeBox = new Color(0.922f, 0.882f, 0.780f);   // #EBE1C7
        private Color chassisBase, effectBoxBase;
        private Image effectBox, chargeGlow;
        private bool chargeCached;
        private float charge;

        /// <summary>
        /// Ein einziger Wert treibt drei Dinge zugleich — deshalb liest sich eine
        /// Aktivierung sofort: der Rahmen wandert von seiner Kantenfarbe nach
        /// #F8EED6, ein Innenschein wächst auf 20 px, und die Pergament-Effektbox
        /// hellt auf. Dass der Textkasten aufleuchtet, ist die klarste mögliche
        /// Aussage „dieser Effekt passiert jetzt".
        /// </summary>
        public void SetCharge(float amount)
        {
            charge = Mathf.Clamp01(amount);
            if (!chargeCached)
            {
                chargeCached = true;
                if (chassisImage != null) chassisBase = chassisImage.color;
                if (effectText != null) effectBox = effectText.transform.parent != null
                    ? effectText.transform.parent.GetComponent<Image>() : null;
                if (effectBox != null) effectBoxBase = effectBox.color;
                chargeGlow = BuildChargeGlow();
            }

            if (chassisImage != null)
                chassisImage.color = Color.Lerp(chassisBase, ChargeInk, charge);
            if (effectBox != null)
                effectBox.color = Color.Lerp(effectBoxBase, ChargeBox, charge);
            if (chargeGlow != null)
            {
                chargeGlow.gameObject.SetActive(charge > 0.002f);
                chargeGlow.color = new Color(ChargeInk.r, ChargeInk.g, ChargeInk.b, 0.55f * charge);
                float inset = -20f * charge;
                chargeGlow.rectTransform.offsetMin = new Vector2(inset, inset);
                chargeGlow.rectTransform.offsetMax = new Vector2(-inset, -inset);
            }
        }

        /// <summary>Setzt die Ladung zurück — nach jeder Aktivierung aufrufen.</summary>
        public void ClearCharge()
        {
            if (!chargeCached) return;
            SetCharge(0f);
        }

        private Image BuildChargeGlow()
        {
            var skin = TransitionSkin.Load();
            if (skin == null || skin.glow == null) return null;
            var go = new GameObject("~ChargeGlow", typeof(RectTransform));
            go.layer = gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            rect.SetAsFirstSibling();                 // hinter der Karte, sonst frisst er das Artwork
            var image = go.AddComponent<Image>();
            image.sprite = skin.glow;
            image.raycastTarget = false;
            image.color = new Color(1f, 1f, 1f, 0f);
            go.SetActive(false);
            return image;
        }

        // ---- Einfärben (Zerstörung: der Rahmen wird zu Asche) ----

        private Graphic[] tintTargets;
        private Color[] tintBase;

        /// <summary>
        /// Färbt die ganze Karte um. <paramref name="drain"/> zieht zusätzlich die
        /// Sättigung heraus — eine zerfallende Karte soll sichtbar aufhören, eine
        /// lebende Karte zu sein. Gedacht für Wegwerf-Kopien (Splitter), nicht für
        /// Karten, die danach weiterspielen.
        /// </summary>
        public void SetTint(Color tint, float drain)
        {
            if (tintTargets == null)
            {
                tintTargets = GetComponentsInChildren<Graphic>(true);
                tintBase = new Color[tintTargets.Length];
                for (int i = 0; i < tintTargets.Length; i++) tintBase[i] = tintTargets[i].color;
            }

            float bleed = Mathf.Clamp01(drain) * 0.9f;
            for (int i = 0; i < tintTargets.Length; i++)
            {
                if (tintTargets[i] == null) continue;
                var original = tintBase[i];
                // Entsättigen über die Helligkeit — ohne eigenen Shader ist das
                // die ehrlichste Annäherung an saturate(1 − drain × 0.9)
                float grey = original.r * 0.299f + original.g * 0.587f + original.b * 0.114f;
                var drained = Color.Lerp(original, new Color(grey, grey, grey, original.a), bleed);
                tintTargets[i].color = new Color(
                    drained.r * tint.r, drained.g * tint.g, drained.b * tint.b, drained.a * tint.a);
            }
        }

        /// <summary>Outline an/aus. Ohne Farbe: gelbe Standard-Outline (Ziele/Aktionen).</summary>
        public void SetHighlight(bool active) => SetHighlight(active, TargetHighlight);

        public void SetHighlight(bool active, Color color)
        {
            if (highlightFrame != null)
            {
                if (active)
                {
                    if (highlightImage == null) highlightImage = highlightFrame.GetComponent<Image>();
                    if (highlightImage != null) highlightImage.color = color;
                }
                highlightFrame.SetActive(active);
            }
            if (cHighlight != null)
            {
                if (active)
                {
                    var image = cHighlight.GetComponent<Image>();
                    if (image != null) image.color = color;
                }
                cHighlight.SetActive(active);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.dragging) return;
            Clicked?.Invoke(this);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            Hovered?.Invoke(this);
            SfxManager.CardHover();
            if (HoverLift && !dragging) SetLift(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Unhovered?.Invoke(this);
            if (HoverLift) SetLift(false);
        }

        // ---- Hover-Lift: Handkarte hebt sich leicht an (22px, Scale 1.05, ~0.1s ease-out) ----

        private void SetLift(bool up)
        {
            if (lifted == up) return;
            lifted = up;
            var rect = (RectTransform)transform;
            if (up && !liftBaseCaptured)
            {
                liftBasePos = rect.anchoredPosition;
                liftBaseCaptured = true;
            }
            if (!liftBaseCaptured) return;
            if (liftRoutine != null) StopCoroutine(liftRoutine);
            liftRoutine = StartCoroutine(LiftRoutine(up));
        }

        private System.Collections.IEnumerator LiftRoutine(bool up)
        {
            var rect = (RectTransform)transform;
            Vector2 targetPos = up ? liftBasePos + new Vector2(0f, 22f) : liftBasePos;
            Vector3 targetScale = Vector3.one * (up ? 1.05f : 1f);
            Vector2 startPos = rect.anchoredPosition;
            Vector3 startScale = transform.localScale;
            const float duration = 0.1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (dragging) yield break; // ab hier übernimmt der Drag die Position
                elapsed += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(elapsed / duration);
                k = 1f - (1f - k) * (1f - k); // ease-out
                rect.anchoredPosition = Vector2.Lerp(startPos, targetPos, k);
                transform.localScale = Vector3.Lerp(startScale, targetScale, k);
                yield return null;
            }
            rect.anchoredPosition = targetPos;
            transform.localScale = targetScale;
        }

        // ---- Drag & Drop: nur eigene, offene Handkarten ----

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = Instance != null && !HiddenFace && Instance.Zone == ZoneType.Hand;
            if (!dragging) return;

            preDragScale = transform.localScale;
            transform.SetParent(transform.root, true);
            transform.SetAsLastSibling();
            transform.localScale = preDragScale * dragScale;
            DragStarted?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            transform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragging) return;
            dragging = false;
            transform.localScale = preDragScale;
            DragEnded?.Invoke(this, eventData.position);
        }
    }
}
