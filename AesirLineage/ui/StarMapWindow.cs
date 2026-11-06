using Jotunn.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using AesirLineage.common;
using AesirLineage.modules;
using AesirLineage.starmap;
using static AesirLineage.common.Deities;
using AesirLineage.visuals;

namespace AesirLineage.ui {
    /// <summary>
    /// The star map screen: one deity's constellation around their portrait, a column of deity tabs,
    /// and tooltips that preview what each star would change. Built in code under Jotunn's front GUI
    /// the first time it opens, and again after a scene change destroys it.
    /// </summary>
    internal sealed class StarMapWindow : MonoBehaviour {
        // Layout at zoom 1, in canvas units. Ring 1 sits just outside the portrait, so the deity stays
        // the centrepiece however large it is drawn; each further ring adds RingSpacing.
        private const float PortraitSize = 300f;
        private const float InnerRadius = PortraitSize * 0.5f + 55f;
        private const float RingSpacing = 115f;
        // Room kept around the outermost stars when fitting the map to the view.
        private const float FitMargin = 90f;
        private const float TabWidth = 190f;
        // Eight deity tabs fit under the header at 1080 lines.
        private const float TabHeight = 76f;
        private const float TabGap = 6f;
        private const float HeaderHeight = 124f;
        private const float FooterHeight = 48f;
        private const float MinZoom = 0.4f;
        private const float MaxZoom = 2.2f;
        private const float ZoomStep = 1.15f;
        // Wide enough for each power's extended description to wrap into a few lines.
        private const float TooltipWidth = 440f;

        private static readonly Color LockedLink = new Color(0.5f, 0.55f, 0.65f, 0.22f);
        private static readonly Color Muted = new Color(0.6f, 0.62f, 0.68f);

        private static StarMapWindow instance;
        private static Deity lastDeity = Deity.Gefjun;

        private class DeityTab {
            public Deity Deity;
            public Image Background;
            public Image Halo;
            public Text Points;
            public readonly List<Image> Pips = new List<Image>();
            public Color Primary;
            public Color Glow;
        }

        private RectTransform root;
        private Canvas canvas;
        private RectTransform viewport;
        private RectTransform content;
        private RectTransform nodeLayer;
        private RectTransform tabColumn;
        private RectTransform tooltip;
        private RawImage starfield;
        private RawImage nebulaA;
        private RawImage nebulaB;
        private LinkGraphic haloLinks;
        private LinkGraphic coreLinks;
        private Image portrait;
        private Image portraitGlow;
        private RawImage runeRingInner;
        private RawImage runeRingOuter;
        // Through the apex stars; lit by the player's Ascension with the deity.
        private RawImage apexRing;
        private LooksPanel looks;
        private GameObject looksButton;
        // Additive glows and flowing links, from the bundle's shaders; null leaves the default UI material.
        private Material additive;
        private Material linkFlow;
        private Text title;
        private Text subtitle;
        private Text pointsText;
        private Text tooltipText;
        private AudioSource sound;

        private readonly List<StarNodeView> nodeViews = new List<StarNodeView>();
        private readonly List<DeityTab> tabs = new List<DeityTab>();
        private readonly float[] previewTotals = new float[PowerInfo.Count];
        private readonly float[] previewDormant = new float[PowerInfo.Count];

        private Deity current;
        private StarMap currentMap;
        private Color primary = Color.white;
        private Color secondary = Color.white;
        private Color glow = Color.white;
        private float zoom = 1f;
        // The map's extent around the portrait, in map units.
        private Rect mapBounds;
        private float nebulaDrift;
        private int shownMapRevision = -1;
        private int shownPlayerRevision = -1;
        private bool holdingInput;
        private StarNodeView hoveredView;
        // Distance from the root along the links, so light flows outward from the deity.
        private readonly Dictionary<StarNode, float> rootDistance = new Dictionary<StarNode, float>();

        private class Spark {
            public RectTransform Rect;
            public Vector2 From;
            public Vector2 To;
            public float Start;
        }

        private const float SparkSeconds = 0.4f;
        private readonly List<Spark> sparks = new List<Spark>();

        internal static bool IsOpen => instance != null && instance.gameObject.activeSelf;

        /// <summary>
        /// The frame the map last closed on, so a panel under it that also closes on Escape in
        /// LateUpdate can tell the keypress was already used.
        /// </summary>
        internal static int LastClosedFrame { get; private set; } = -1;

        internal static void Show(Deity? deity = null) {
            if (Player.m_localPlayer == null || GUIManager.CustomGUIFront == null) { return; }
            if (instance == null) { instance = Create(); }
            instance.Open(deity ?? lastDeity);
        }

        internal static void Hide() {
            if (instance != null) { instance.Close(); }
        }

        /// <summary>Called every frame from the plugin: the hotkey, and closing when the player is gone.</summary>
        internal static void UpdateHotkey() {
            if (IsOpen && Player.m_localPlayer == null) {
                Hide();
                return;
            }
            if (ValConfig.StarMapKey == null || !ValConfig.StarMapKey.Value.IsDown()) { return; }
            if (IsOpen) {
                Hide();
            } else if (CanOpenFromHotkey()) {
                Show();
            }
        }

        // Not while typing, or while another screen has the cursor.
        private static bool CanOpenFromHotkey() {
            return Player.m_localPlayer != null
                && !global::Console.IsVisible()
                && !(Chat.instance != null && Chat.instance.HasFocus())
                && !TextInput.IsVisible()
                && !Menu.IsVisible()
                && !Minimap.IsOpen()
                && !InventoryGui.IsVisible()
                && !StoreGui.IsVisible()
                && !Hud.IsPieceSelectionVisible();
        }

        private static StarMapWindow Create() {
            GameObject go = new GameObject("AesirLineageStarMap", typeof(RectTransform));
            go.layer = 5;
            go.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
            StarMapWindow window = go.AddComponent<StarMapWindow>();
            window.Construct();
            return window;
        }

        private void Open(Deity deity) {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            // A server with looks off (EnableLooks) has no Looks button.
            looksButton.SetActive(ValConfig.EnableLooks.Value);
            if (!ValConfig.EnableLooks.Value) { looks.gameObject.SetActive(false); }
            if (!holdingInput) {
                GUIManager.BlockInput(true);
                holdingInput = true;
            }
            SelectDeity(deity, fit: true);
        }

        private void Close() {
            LastClosedFrame = Time.frameCount;
            hoveredView = null;
            tooltip.gameObject.SetActive(false);
            gameObject.SetActive(false);
            ReleaseInput();
        }

        // Jotunn counts block requests, so every block is paired with exactly one release.
        private void ReleaseInput() {
            if (!holdingInput) { return; }
            holdingInput = false;
            GUIManager.BlockInput(false);
        }

        private void OnDestroy() {
            ReleaseInput();
        }

        private Camera EventCamera => canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        // ---- Construction ----

        private void Construct() {
            root = (RectTransform)transform;
            Stretch(root);
            canvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (canvas != null) { canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1; }
            additive = DeityVfx.UIAdditive != null && DeityVfx.UIAdditive.isSupported ? new Material(DeityVfx.UIAdditive) : null;
            linkFlow = DeityVfx.UILinkFlow != null && DeityVfx.UILinkFlow.isSupported ? new Material(DeityVfx.UILinkFlow) : null;

            Image backdrop = gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.015f, 0.018f, 0.04f, 0.96f);
            backdrop.raycastTarget = true;
            sound = StarMapSounds.CreateSource(gameObject);

            starfield = NewRawImage(root, "Starfield", StarMapArt.Starfield, new Color(1f, 1f, 1f, 0.85f));
            Stretch(starfield.rectTransform);
            nebulaA = NewRawImage(root, "NebulaA", StarMapArt.Nebula, Color.clear);
            Stretch(nebulaA.rectTransform);
            nebulaB = NewRawImage(root, "NebulaB", StarMapArt.Nebula, Color.clear);
            Stretch(nebulaB.rectTransform);

            viewport = NewRect("Viewport", root);
            Stretch(viewport, TabWidth + 40f, FooterHeight, 20f, HeaderHeight);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hitArea = viewport.gameObject.AddComponent<Image>();
            hitArea.color = Color.clear;
            hitArea.raycastTarget = true;
            viewport.gameObject.AddComponent<StarMapViewport>().Window = this;

            content = NewRect("Content", viewport);
            Place(content, Center, Center, Vector2.zero, Vector2.zero);
            haloLinks = NewLinks(content, "LinkHalo", 16f);
            haloLinks.material = additive;
            coreLinks = NewLinks(content, "LinkCore", 3.5f);
            coreLinks.material = linkFlow;
            portraitGlow = NewImage(content, "PortraitGlow", StarMapArt.SoftGlow, Color.clear);
            portraitGlow.material = additive;
            Place(portraitGlow.rectTransform, Center, Center, Vector2.zero, Vector2.one * PortraitSize * 2.4f);
            runeRingOuter = NewRawImage(content, "RuneRingOuter", null, Color.clear);
            runeRingOuter.material = additive;
            Place(runeRingOuter.rectTransform, Center, Center, Vector2.zero, Vector2.one * PortraitSize * 1.7f);
            runeRingInner = NewRawImage(content, "RuneRingInner", null, Color.clear);
            runeRingInner.material = additive;
            Place(runeRingInner.rectTransform, Center, Center, Vector2.zero, Vector2.one * PortraitSize * 1.36f);
            apexRing = NewRawImage(content, "ApexRing", StarMapArt.ApexRing, Color.clear);
            apexRing.material = additive;
            Place(apexRing.rectTransform, Center, Center, Vector2.zero, Vector2.zero);
            portrait = NewImage(content, "Portrait", null, Color.white);
            portrait.preserveAspect = true;
            Place(portrait.rectTransform, Center, Center, Vector2.zero, Vector2.one * PortraitSize);
            nodeLayer = NewRect("Nodes", content);
            Place(nodeLayer, Center, Center, Vector2.zero, Vector2.zero);

            RawImage vignette = NewRawImage(root, "Vignette", StarMapArt.Vignette, Color.white);
            Stretch(vignette.rectTransform);

            title = NewText(root, "Title", HeaderFont, 44, Color.white, TextAnchor.MiddleCenter);
            Place(title.rectTransform, Top, Top, new Vector2(0f, -12f), new Vector2(1000f, 58f));
            subtitle = NewText(root, "Subtitle", BodyFont, 16, GUIManager.Instance.ValheimBeige, TextAnchor.MiddleCenter);
            Place(subtitle.rectTransform, Top, Top, new Vector2(0f, -68f), new Vector2(1000f, 24f));
            pointsText = NewText(root, "Points", BoldFont, 19, GUIManager.Instance.ValheimYellow, TextAnchor.MiddleCenter);
            Place(pointsText.rectTransform, Top, Top, new Vector2(0f, -94f), new Vector2(1000f, 26f));

            Text hint = NewText(root, "Hint", BodyFont, 15, Muted, TextAnchor.MiddleCenter);
            Place(hint.rectTransform, Bottom, Bottom, new Vector2(0f, 12f), new Vector2(1400f, 24f));
            hint.text = Localization.instance.Localize("$al_starmap_hint_locked");

            tabColumn = NewRect("Tabs", root);
            Place(tabColumn, TopLeft, TopLeft, new Vector2(20f, -HeaderHeight), new Vector2(TabWidth, 8 * (TabHeight + TabGap)));

            GameObject close = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize("$al_close"),
                parent: root,
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                position: new Vector2(-46f, -46f),
                width: 56f,
                height: 56f);
            close.GetComponent<Button>().onClick.AddListener(Close);

            looksButton = GUIManager.Instance.CreateButton(
                text: Localization.instance.Localize("$al_looks_button"),
                parent: root,
                anchorMin: new Vector2(1f, 1f),
                anchorMax: new Vector2(1f, 1f),
                position: new Vector2(-150f, -46f),
                width: 120f,
                height: 46f);
            looksButton.GetComponent<Button>().onClick.AddListener(ToggleLooks);
            looks = LooksPanel.Create(root);
            looks.gameObject.SetActive(false);

            tooltip = NewRect("Tooltip", root);
            Place(tooltip, Center, new Vector2(0f, 1f), Vector2.zero, new Vector2(TooltipWidth, 10f));
            Image tooltipBack = tooltip.gameObject.AddComponent<Image>();
            tooltipBack.color = new Color(0.03f, 0.035f, 0.06f, 0.95f);
            tooltipBack.raycastTarget = false;
            VerticalLayoutGroup layout = tooltip.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 14);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            tooltip.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            tooltipText = NewText(tooltip, "Text", BodyFont, 16, Color.white, TextAnchor.UpperLeft);
            tooltip.gameObject.SetActive(false);

            BuildTabs();
        }

        private void BuildTabs() {
            foreach (DeityTab tab in tabs) { Destroy(tab.Background.gameObject); }
            tabs.Clear();
            int index = 0;
            foreach (Deity deity in Enum.GetValues(typeof(Deity))) {
                if (!StarMaps.TryGet(deity, out StarMap map)) { continue; }
                DeityTab tab = new DeityTab() {
                    Deity = deity,
                    Primary = StarMapArt.ParseColor(map.Definition.Palette?.Primary, Color.white),
                    Glow = StarMapArt.ParseColor(map.Definition.Palette?.Glow, Color.white),
                };
                RectTransform rt = NewRect($"Tab_{deity}", tabColumn);
                Place(rt, TopLeft, TopLeft, new Vector2(0f, -index * (TabHeight + TabGap)), new Vector2(TabWidth, TabHeight));
                tab.Background = rt.gameObject.AddComponent<Image>();
                tab.Background.raycastTarget = true;
                Button button = rt.gameObject.AddComponent<Button>();
                button.targetGraphic = tab.Background;
                button.onClick.AddListener(() => SelectDeity(deity, fit: true));

                tab.Halo = NewImage(rt, "Halo", StarMapArt.SoftGlow, Color.clear);
                tab.Halo.material = additive;
                Place(tab.Halo.rectTransform, Left, Center, new Vector2(42f, 0f), new Vector2(104f, 104f));
                Image face = NewImage(rt, "Portrait", DeityConfiguration.TryGetValue(deity, out DeityDetails details) ? details.Image : null, Color.white);
                face.preserveAspect = true;
                Place(face.rectTransform, Left, Center, new Vector2(42f, 0f), new Vector2(72f, 72f));
                Text name = NewText(rt, "Name", HeaderFont, 22, GUIManager.Instance.ValheimBeige, TextAnchor.MiddleLeft);
                Place(name.rectTransform, Left, Left, new Vector2(86f, 10f), new Vector2(TabWidth - 92f, 30f));
                name.text = Compendium.DeityName(deity);
                tab.Points = NewText(rt, "Points", BoldFont, 14, Muted, TextAnchor.MiddleLeft);
                Place(tab.Points.rectTransform, Left, Left, new Vector2(86f, -16f), new Vector2(TabWidth - 92f, 22f));
                // A star in the corner for each Ascension the deity allows, lit as the player reaches it.
                for (int pip = 0; pip < Mathf.Min(Ascension.Max, 6); pip++) {
                    Image star = NewImage(rt, "Ascension", StarMapArt.Star, Color.clear);
                    Place(star.rectTransform, TopLeft, Center, new Vector2(TabWidth - 14f - pip * 15f, -13f), new Vector2(16f, 16f));
                    tab.Pips.Add(star);
                }
                tabs.Add(tab);
                index++;
            }
        }

        private void SelectDeity(Deity deity, bool fit) {
            if (!StarMaps.TryGet(deity, out StarMap map)) {
                map = StarMaps.All.FirstOrDefault();
                if (map == null) { return; }
                deity = map.Deity;
            }
            current = deity;
            lastDeity = deity;
            currentMap = map;
            primary = StarMapArt.ParseColor(map.Definition.Palette?.Primary, Color.white);
            secondary = StarMapArt.ParseColor(map.Definition.Palette?.Secondary, Color.white);
            glow = StarMapArt.ParseColor(map.Definition.Palette?.Glow, Color.white);

            Sprite face = DeityConfiguration.TryGetValue(deity, out DeityDetails details) ? details.Image : null;
            portrait.sprite = face;
            portrait.enabled = face != null;
            nebulaA.color = StarMapArt.WithAlpha(primary, 0.3f);
            nebulaB.color = StarMapArt.WithAlpha(secondary, 0.2f);
            title.text = Compendium.DeityName(deity);
            Texture2D ring = StarMapArt.RuneRing(map.Definition.Rune);
            runeRingInner.texture = ring;
            runeRingOuter.texture = ring;
            runeRingInner.color = StarMapArt.WithAlpha(glow, 0.55f);
            runeRingOuter.color = StarMapArt.WithAlpha(primary, 0.22f);
            title.color = Color.Lerp(primary, Color.white, 0.3f);
            subtitle.text = details != null ? Localization.instance.Localize(details.DescriptionLocalKey) : "";

            BuildNodes();
            Refresh();
            if (fit) { FitToView(); }
        }

        private void BuildNodes() {
            foreach (StarNodeView view in nodeViews) { Destroy(view.gameObject); }
            nodeViews.Clear();
            hoveredView = null;
            tooltip.gameObject.SetActive(false);
            float half = PortraitSize * 0.5f;
            Vector2 min = new Vector2(-half, -half);
            Vector2 max = new Vector2(half, half);
            foreach (StarNode node in currentMap.Nodes.Values) {
                if (node == currentMap.Root) { continue; }
                Vector2 position = Position(node);
                min = Vector2.Min(min, position);
                max = Vector2.Max(max, position);
                nodeViews.Add(CreateNodeView(node, position));
            }
            mapBounds = Rect.MinMaxRect(min.x - FitMargin, min.y - FitMargin, max.x + FitMargin, max.y + FitMargin);
            MeasureFromRoot();
            // The apex stars of the default maps share a ring; with none, there is no ring to draw.
            float apexRadius = currentMap.Nodes.Values.Where(n => n.Tier == NodeTier.Apex).Select(n => Position(n).magnitude).DefaultIfEmpty(0f).Max();
            apexRing.enabled = apexRadius > 0f;
            apexRing.rectTransform.sizeDelta = Vector2.one * apexRadius * 2f;
            shownMapRevision = StarMaps.Revision;
        }

        // Shortest distance from the root along the links; the maps are small, so a simple relaxation does.
        private void MeasureFromRoot() {
            rootDistance.Clear();
            if (currentMap.Root == null) { return; }
            rootDistance[currentMap.Root] = 0f;
            Queue<StarNode> open = new Queue<StarNode>();
            open.Enqueue(currentMap.Root);
            while (open.Count > 0) {
                StarNode node = open.Dequeue();
                float here = rootDistance[node];
                foreach (StarNode next in node.Neighbours) {
                    float there = here + Vector2.Distance(Position(node), Position(next));
                    if (!rootDistance.TryGetValue(next, out float known) || there < known - 0.01f) {
                        rootDistance[next] = there;
                        open.Enqueue(next);
                    }
                }
            }
        }

        private float DistanceOf(StarNode node) => rootDistance.TryGetValue(node, out float distance) ? distance : 0f;

        private StarNodeView CreateNodeView(StarNode node, Vector2 position) {
            (float core, float glowSize, float ringSize, float hitSize) = node.Tier switch {
                NodeTier.Major => (52f, 120f, 66f, 58f),
                NodeTier.Oath => (52f, 120f, 72f, 58f),
                NodeTier.Apex => (74f, 170f, 94f, 80f),
                _ => (34f, 78f, 0f, 40f),
            };
            RectTransform rt = NewRect($"Node_{node.Id}", nodeLayer);
            Place(rt, Center, Center, position, new Vector2(hitSize, hitSize));
            Image hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            hit.raycastTarget = true;
            StarNodeView view = rt.gameObject.AddComponent<StarNodeView>();
            view.Node = node;
            view.Window = this;
            view.Glow = NewImage(rt, "Glow", StarMapArt.SoftGlow, Color.clear);
            view.Glow.material = additive;
            Place(view.Glow.rectTransform, Center, Center, Vector2.zero, new Vector2(glowSize, glowSize));
            if (ringSize > 0f) {
                view.Ring = NewImage(rt, "Ring", StarMapArt.Ring, Color.clear);
                Place(view.Ring.rectTransform, Center, Center, Vector2.zero, new Vector2(ringSize, ringSize));
            }
            view.Core = NewImage(rt, "Star", StarMapArt.Star, Color.white);
            Place(view.Core.rectTransform, Center, Center, Vector2.zero, new Vector2(core, core));
            return view;
        }

        private Vector2 Position(StarNode node) {
            if (node == currentMap.Root || node.Definition.Ring <= 0f) { return Vector2.zero; }
            float radians = node.Definition.Angle * Mathf.Deg2Rad;
            float radius = InnerRadius + (node.Definition.Ring - 1f) * RingSpacing;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
        }

        // ---- State ----

        private bool Taken(StarNode node) => node == currentMap.Root || PlayerData.IsAllocated(node);

        private NodeState StateOf(StarNode node) {
            if (PlayerData.IsAllocated(node)) { return NodeState.Taken; }
            if (PlayerData.CanAllocate(node, out _)) { return NodeState.Available; }
            return node.Neighbours.Any(Taken) ? NodeState.Reachable : NodeState.Locked;
        }

        /// <summary>Recolours stars and links and rewrites the header, tabs and any open tooltip.</summary>
        private void Refresh() {
            foreach (StarNodeView view in nodeViews) {
                view.Apply(StateOf(view.Node), primary, secondary, glow);
            }

            haloLinks.Segments.Clear();
            coreLinks.Segments.Clear();
            foreach (StarNode node in currentMap.Nodes.Values) {
                foreach (StarNode other in node.Neighbours) {
                    // Each pair once.
                    if (string.CompareOrdinal(node.Id, other.Id) >= 0) { continue; }
                    bool a = Taken(node);
                    bool b = Taken(other);
                    Color coreColor;
                    Color haloColor;
                    if (a && b) {
                        coreColor = StarMapArt.WithAlpha(Color.Lerp(primary, glow, 0.35f), 0.95f);
                        haloColor = StarMapArt.WithAlpha(glow, 0.5f);
                    } else if (a || b) {
                        coreColor = StarMapArt.WithAlpha(secondary, 0.6f);
                        haloColor = StarMapArt.WithAlpha(secondary, 0.12f);
                    } else {
                        coreColor = LockedLink;
                        haloColor = Color.clear;
                    }
                    StarNode near = DistanceOf(node) <= DistanceOf(other) ? node : other;
                    StarNode far = near == node ? other : node;
                    Vector2 from = Position(near);
                    Vector2 to = Position(far);
                    float flow = a && b ? 1f : 0f;
                    coreLinks.Segments.Add(new LinkGraphic.Segment() { From = from, To = to, Color = coreColor, FromDistance = DistanceOf(near), Flow = flow });
                    haloLinks.Segments.Add(new LinkGraphic.Segment() { From = from, To = to, Color = haloColor, FromDistance = DistanceOf(near), Flow = flow });
                }
            }
            haloLinks.Rebuild();
            coreLinks.Rebuild();

            int earned = PlayerData.PointsEarned(current);
            int spent = PlayerData.PointsSpent(current);
            pointsText.text = Localization.instance.Localize("$al_starmap_points", (earned - spent).ToString(), spent.ToString(), earned.ToString());
            int ascension = PlayerData.AscensionLevel(current);
            if (ascension > 0) {
                pointsText.text += "  -  " + Localization.instance.Localize("$al_starmap_ascension", ascension.ToString(), Ascension.Max.ToString());
            }
            apexRing.color = ascension > 0 ? StarMapArt.WithAlpha(glow, Mathf.Min(0.9f, 0.3f + 0.15f * ascension)) : StarMapArt.WithAlpha(LockedLink, 0.3f);
            portraitGlow.color = StarMapArt.WithAlpha(glow, 0.45f);

            foreach (DeityTab tab in tabs) {
                bool selected = tab.Deity == current;
                tab.Background.color = selected ? StarMapArt.WithAlpha(tab.Primary, 0.22f) : new Color(1f, 1f, 1f, 0.05f);
                tab.Halo.color = selected ? StarMapArt.WithAlpha(tab.Glow, 0.55f) : Color.clear;
                int tabEarned = PlayerData.PointsEarned(tab.Deity);
                int tabSpent = PlayerData.PointsSpent(tab.Deity);
                if (tabEarned - tabSpent > 0) {
                    tab.Points.text = Localization.instance.Localize("$al_starmap_tab_spend", (tabEarned - tabSpent).ToString());
                    tab.Points.color = GUIManager.Instance.ValheimYellow;
                } else {
                    tab.Points.text = tabEarned > 0 ? Localization.instance.Localize("$al_starmap_tab_spent", tabSpent.ToString(), tabEarned.ToString()) : "";
                    tab.Points.color = Muted;
                }
                int reached = PlayerData.AscensionLevel(tab.Deity);
                for (int pip = 0; pip < tab.Pips.Count; pip++) {
                    tab.Pips[pip].color = pip < reached ? StarMapArt.WithAlpha(Color.Lerp(tab.Glow, Color.white, 0.3f), 1f) : new Color(1f, 1f, 1f, 0.14f);
                }
            }

            shownPlayerRevision = PlayerData.Revision;
            if (looks.gameObject.activeSelf) { looks.Refresh(); }
            if (hoveredView != null) { ShowTooltip(hoveredView); }
        }

        // ---- Frame ----

        private void Update() {
            if (Player.m_localPlayer == null) {
                Close();
                return;
            }
            if (StarMaps.Revision != shownMapRevision) {
                // The server sent new maps, or the file changed.
                BuildTabs();
                SelectDeity(current, fit: false);
            } else if (PlayerData.Revision != shownPlayerRevision) {
                Refresh();
            }

            float time = Time.unscaledTime;
            portraitGlow.transform.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(time * 0.9f));
            portraitGlow.canvasRenderer.SetAlpha(0.75f + 0.25f * Mathf.Sin(time * 1.2f));
            haloLinks.canvasRenderer.SetAlpha(0.7f + 0.3f * Mathf.Sin(time * 2.2f));
            runeRingInner.rectTransform.localRotation = Quaternion.Euler(0f, 0f, time * 6f);
            runeRingOuter.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -time * 3.5f);
            apexRing.rectTransform.localRotation = Quaternion.Euler(0f, 0f, time * 1.2f);
            UpdateSparks(time);
            foreach (StarNodeView view in nodeViews) { view.Animate(time); }

            nebulaDrift += Time.unscaledDeltaTime * 0.004f;
            UpdateParallax();
            if (tooltip.gameObject.activeSelf) { PositionTooltip(); }
        }

        // Escape is handled after every Update: Jotunn's input block keeps the game menu from opening on
        // it while the map is open, but only if the map is still open when Menu.Update runs.
        private void LateUpdate() {
            if (ZInput.GetKeyDown(KeyCode.Escape, logWarning: false)) { Close(); }
        }

        // ---- Pan and zoom ----

        internal void Pan(Vector2 screenDelta) {
            float scale = canvas != null ? canvas.scaleFactor : 1f;
            content.anchoredPosition += screenDelta / scale;
            ClampPan();
        }

        internal void ZoomAt(Vector2 screenPosition, float wheel) {
            if (Mathf.Approximately(wheel, 0f)) { return; }
            float target = Mathf.Clamp(zoom * (wheel > 0f ? ZoomStep : 1f / ZoomStep), MinZoom, MaxZoom);
            if (Mathf.Approximately(target, zoom)) { return; }
            // Keep the point under the cursor where it is.
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screenPosition, EventCamera, out Vector2 before);
            SetZoom(target);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(content, screenPosition, EventCamera, out Vector2 after);
            content.anchoredPosition += (after - before) * zoom;
            ClampPan();
        }

        private void SetZoom(float value) {
            zoom = value;
            content.localScale = Vector3.one * zoom;
        }

        // The middle of the view always shows some part of the map.
        private void ClampPan() {
            Vector2 position = content.anchoredPosition;
            content.anchoredPosition = new Vector2(
                Mathf.Clamp(position.x, -mapBounds.xMax * zoom, -mapBounds.xMin * zoom),
                Mathf.Clamp(position.y, -mapBounds.yMax * zoom, -mapBounds.yMin * zoom));
        }

        // Fits the whole map, not a circle around the portrait: arms rarely reach equally far each way.
        private void FitToView() {
            Rect area = viewport.rect;
            float fit = area.height > 1f ? Mathf.Min(area.width / mapBounds.width, area.height / mapBounds.height) : 0.8f;
            SetZoom(Mathf.Clamp(fit, MinZoom, 1.25f));
            content.anchoredPosition = -mapBounds.center * zoom;
        }

        // The starfield and nebulae slide a little with the map, so it feels deep.
        private void UpdateParallax() {
            Vector2 pan = content.anchoredPosition;
            Rect area = root.rect;
            starfield.uvRect = new Rect(-pan.x / 2400f, -pan.y / 2400f, area.width / 512f, area.height / 512f);
            nebulaA.uvRect = new Rect(-pan.x / 5000f + nebulaDrift, -pan.y / 5000f, 1.3f, 0.75f);
            nebulaB.uvRect = new Rect(0.37f - pan.x / 7000f - nebulaDrift * 0.7f, 0.61f - pan.y / 7000f, 0.9f, 0.55f);
        }

        // ---- Nodes ----

        internal void OnNodeHover(StarNodeView view) {
            hoveredView = view;
            ShowTooltip(view);
        }

        internal void OnNodeHoverEnd(StarNodeView view) {
            if (hoveredView != view) { return; }
            hoveredView = null;
            tooltip.gameObject.SetActive(false);
        }

        internal void TryAllocate(StarNodeView view) {
            if (PlayerData.IsAllocated(view.Node)) { return; }
            if (PlayerData.Allocate(view.Node, out _)) {
                view.Celebrate();
                StarMapSounds.PlayTaken(sound, view.Node.Tier);
                foreach (StarNode neighbour in view.Node.Neighbours) {
                    if (Taken(neighbour)) { LaunchSpark(Position(neighbour), Position(view.Node)); }
                }
                Refresh();
            } else {
                // The tooltip already says why.
                ShowTooltip(view);
            }
        }

        // A burst of light that runs along a link into a star just taken.
        private void LaunchSpark(Vector2 from, Vector2 to) {
            Image image = NewImage(nodeLayer, "Spark", StarMapArt.SoftGlow, StarMapArt.WithAlpha(Color.Lerp(glow, Color.white, 0.4f), 1f));
            image.material = additive;
            Place(image.rectTransform, Center, Center, from, new Vector2(46f, 46f));
            sparks.Add(new Spark() { Rect = image.rectTransform, From = from, To = to, Start = Time.unscaledTime });
        }

        private void UpdateSparks(float time) {
            for (int i = sparks.Count - 1; i >= 0; i--) {
                Spark spark = sparks[i];
                float t = (time - spark.Start) / SparkSeconds;
                if (t >= 1f || spark.Rect == null) {
                    if (spark.Rect != null) { Destroy(spark.Rect.gameObject); }
                    sparks.RemoveAt(i);
                    continue;
                }
                float eased = 1f - (1f - t) * (1f - t);
                spark.Rect.anchoredPosition = Vector2.Lerp(spark.From, spark.To, eased);
                spark.Rect.localScale = Vector3.one * (0.7f + 0.6f * t);
            }
        }

        private void ToggleLooks() {
            looks.gameObject.SetActive(!looks.gameObject.activeSelf);
            if (looks.gameObject.activeSelf) {
                looks.transform.SetAsLastSibling();
                looks.Refresh();
            }
        }

        private void ShowTooltip(StarNodeView view) {
            tooltipText.text = TooltipFor(view.Node);
            tooltip.gameObject.SetActive(true);
            tooltip.SetAsLastSibling();
            LayoutRebuilder.ForceRebuildLayoutImmediate(tooltip);
            PositionTooltip();
        }

        private void PositionTooltip() {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, ZInput.pointerPosition, EventCamera, out Vector2 pointer);
            Rect area = root.rect;
            Vector2 size = tooltip.rect.size;
            Vector2 position = pointer + new Vector2(26f, -26f);
            if (position.x + size.x > area.xMax - 8f) { position.x = pointer.x - 26f - size.x; }
            if (position.y - size.y < area.yMin + 8f) { position.y = area.yMin + 8f + size.y; }
            tooltip.anchoredPosition = position;
        }

        /// <summary>
        /// Name, tier and cost, then each power with how it works. A star not yet taken shows each power as
        /// it would be after taking it, with today's value beside it; a taken star shows the powers as they
        /// are now and its own share. Then what clicking does, or why it can't, or that the star is locked.
        /// </summary>
        private string TooltipFor(StarNode node) {
            Localization loc = Localization.instance;
            bool taken = PlayerData.IsAllocated(node);
            if (!taken) { PowerSet.Preview(PlayerData.AllAllocated().Append(node), previewTotals, previewDormant); }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"<size=22><b><color=#{ColorUtility.ToHtmlStringRGB(TierColor(node))}>{node.DisplayName}</color></b></size>");
            sb.AppendLine($"<color=#9aa0aa>{loc.Localize("$al_starmap_tier_line", loc.Localize($"$al_tier_{node.Tier}"), node.Cost.ToString())}</color>");
            if (node.Definition.Ascension > 0) {
                sb.AppendLine($"<color=#c9a0ff>{loc.Localize("$al_reason_ascension", node.Definition.Ascension.ToString())}</color>");
            }
            sb.AppendLine();
            foreach (KeyValuePair<Power, float> grant in node.Grants) {
                Power power = grant.Key;
                PowerInfo info = PowerInfo.Of(power);
                float now = PowerSet.Get(power);
                float shown = taken ? now : previewTotals[(int)power];
                float dormant = taken ? PowerSet.Dormant(power) : previewDormant[(int)power];
                string line;
                if (shown > 0f) {
                    line = Compendium.DescribePower(power, shown);
                } else if (dormant > 0f) {
                    line = $"{loc.Localize(info.NameKey)}: {loc.Localize("$al_power_dormant", dormant.ToString("0.##"))}";
                } else if (taken) {
                    line = Compendium.DescribePower(power, shown);
                } else {
                    line = loc.Localize("$al_starmap_power_ends", loc.Localize(info.NameKey));
                }
                string aside = "";
                if (!taken && !Mathf.Approximately(now, shown)) {
                    aside = $" <color=#8a8f99>({loc.Localize("$al_starmap_now", now.ToString("0.##"))})</color>";
                } else if (taken && (PowerSet.SourcesOf(power).Count > 1 || Boons.NamesGranting(power).Any())) {
                    // Other stars or a boon add to it too; say how much is this star's.
                    aside =$" <color=#8a8f99>({loc.Localize("$al_starmap_from_star", grant.Value.ToString("0.##"))})</color>";
                }
                sb.AppendLine($"<color={(info.IsOath ? "#ff7a6e" : "#b9f2ff")}>{line}</color>{aside}");
                string details = Compendium.PowerDetails(power);
                if (details != null) { sb.AppendLine($"<size=14><color=#a3aab5>{details}</color></size>"); }
            }
            foreach (CosmeticSlot slot in ValConfig.EnableLooks.Value ? node.Looks : Enumerable.Empty<CosmeticSlot>()) {
                sb.AppendLine($"<color=#c9a0ff>{loc.Localize("$al_starmap_look", Compendium.DeityName(node.Deity), loc.Localize($"$al_slot_{slot}"))}</color>");
            }
            sb.AppendLine();
            if (taken) {
                sb.Append($"<color=#8a8f99>{loc.Localize("$al_starmap_locked", Compendium.DeityName(node.Deity))}</color>");
            } else {
                sb.Append(PlayerData.CanAllocate(node, out string reason)
                    ? $"<color=#ffe27a>{loc.Localize("$al_starmap_take")}</color>"
                    : $"<color=#8a8f99>{loc.Localize(reason)}</color>");
            }
            return sb.ToString();
        }

        private Color TierColor(StarNode node) {
            switch (node.Tier) {
                case NodeTier.Oath: return new Color(1f, 0.48f, 0.43f);
                case NodeTier.Apex: return Color.Lerp(glow, Color.white, 0.2f);
                case NodeTier.Major: return Color.Lerp(primary, Color.white, 0.35f);
                default: return Color.white;
            }
        }

        // ---- Small builders ----

        private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 Top = new Vector2(0.5f, 1f);
        private static readonly Vector2 Bottom = new Vector2(0.5f, 0f);
        private static readonly Vector2 Left = new Vector2(0f, 0.5f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);

        private static Font HeaderFont => GUIManager.Instance.NorseBold ?? GUIManager.Instance.AveriaSerifBold;
        private static Font BoldFont => GUIManager.Instance.AveriaSerifBold;
        private static Font BodyFont => GUIManager.Instance.AveriaSerif;

        private static RectTransform NewRect(string name, Transform parent) {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            RectTransform rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static void Stretch(RectTransform rt, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f) {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = Center;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size) {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static Image NewImage(Transform parent, string name, Sprite sprite, Color color) {
            Image image = NewRect(name, parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage NewRawImage(Transform parent, string name, Texture texture, Color color) {
            RawImage image = NewRect(name, parent).gameObject.AddComponent<RawImage>();
            image.texture = texture;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static LinkGraphic NewLinks(Transform parent, string name, float width) {
            RectTransform rt = NewRect(name, parent);
            // Large, so RectMask2D never culls it while any link is on screen.
            Place(rt, Center, Center, Vector2.zero, new Vector2(8000f, 8000f));
            LinkGraphic links = rt.gameObject.AddComponent<LinkGraphic>();
            links.Width = width;
            links.Texture = StarMapArt.LinkFalloff;
            links.raycastTarget = false;
            return links;
        }

        private static Text NewText(Transform parent, string name, Font font, int size, Color color, TextAnchor anchor) {
            Text text = NewRect(name, parent).gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = size;
            text.color = color;
            text.alignment = anchor;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
            return text;
        }
    }
}
