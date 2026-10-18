using System;
using System.Linq;
using Core.Services;
using HarmonyLib;
using Items.Model;
using Players.Interface.Services;
using Ships.Interface.Model.Parts;
using Ships.Interface.Model.Parts.StateTypes;
using Ships.Interface.Services;
using StellarIndustries.Core;
using UI.Inventory;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace StellarIndustries.UI
{
    /// <summary>
    /// The chest menu of a storage with more than 10 slots (the industrial container): the
    /// game's grid gets one slot per extra slot, inside a scroll view where the game's 10 slots
    /// were, with a scroll bar in the panel's margin; the mouse wheel scrolls it by one row
    /// while the pointer is over it.
    /// </summary>
    internal static class ContainerMenu
    {
        private static readonly AccessTools.FieldRef<InventoryGrid, InventoryType> Source =
            AccessTools.FieldRefAccess<InventoryGrid, InventoryType>("source");

        /// <summary>The open chest's slot count, or 0 when none is open.</summary>
        public static int OpenSlotCount()
        {
            var players = ServiceLocator.GetService<IPlayersProviderClient>();
            var ships = ServiceLocator.GetService<IShipsClientProvider>();
            if (players == null || ships == null || !players.TryGetLocalPlayerState(out var state) ||
                !ships.TryGetPartState<IStatefulPartState>(new PartContext(state.EntityId, state.PartId), out var partState) || !(partState is IStoragePart storage))
                return 0;
            return storage.SlotCount;
        }

        /// <summary>The scroll view made for the open menu, or null.</summary>
        public static ScrollRect Open { get; private set; }

        public static void Expand(GameObject menu, int count)
        {
            Open = null;
            InventoryGrid grid = menu.GetComponentsInChildren<InventoryGrid>(true).FirstOrDefault(g => Source(g) == InventoryType.Chest);
            InventorySlotUI[] slots = grid != null ? grid.GetComponentsInChildren<InventorySlotUI>(true) : null;
            if (slots == null || slots.Length == 0 || slots.Length >= count)
                return;
            var content = (RectTransform)slots[0].transform.parent;
            var panel = (RectTransform)content.parent;
            // The menu was made this frame: its layout is computed before the slots are measured
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(content);
            if (Diagnostics.DevTest.Active)
                Describe(content);
            // The game's slots, measured where they show (the grid's own rectangle is elsewhere)
            Rect[] boxes = slots.Select(s => Box((RectTransform)s.transform, panel)).ToArray();
            Rect all = boxes[0];
            foreach (Rect b in boxes)
                all = Rect.MinMaxRect(Mathf.Min(all.xMin, b.xMin), Mathf.Min(all.yMin, b.yMin), Mathf.Max(all.xMax, b.xMax), Mathf.Max(all.yMax, b.yMax));
            Vector2 cell = boxes[0].size;
            Vector2 first = boxes[0].center;
            float across = boxes.Where(b => Mathf.Abs(b.center.y - first.y) < 1f).Select(b => b.center.x - first.x).Where(d => d > 1f).DefaultIfEmpty(cell.x).Min();
            float down = boxes.Select(b => first.y - b.center.y).Where(d => d > 1f).DefaultIfEmpty(cell.y).Min();
            int columns = Mathf.Max(1, boxes.Count(b => Mathf.Abs(b.center.y - first.y) < 1f));
            int rows = Mathf.CeilToInt((float)count / columns);

            RectTransform view = View(panel, content.GetSiblingIndex(), all);
            // The grid becomes the scrolled content: its layout is off, every slot is placed by row
            foreach (LayoutGroup group in content.GetComponents<LayoutGroup>())
                group.enabled = false;
            foreach (ContentSizeFitter fitter in content.GetComponents<ContentSizeFitter>())
                fitter.enabled = false;
            content.SetParent(view, false);
            content.localScale = Vector3.one;
            content.localRotation = Quaternion.identity;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, (rows - 1) * down + cell.y);
            for (int i = slots.Length; i < count; i++)
            {
                GameObject copy = UnityEngine.Object.Instantiate(slots[0].gameObject, content);
                copy.name = slots[0].gameObject.name.Split('(')[0].Trim() + " " + i;
            }
            InventorySlotUI[] every = content.GetComponentsInChildren<InventorySlotUI>(true);
            for (int i = 0; i < every.Length; i++)
            {
                var rt = (RectTransform)every[i].transform;
                Vector3 scale = rt.localScale;
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(cell.x / Mathf.Max(0.01f, scale.x), cell.y / Mathf.Max(0.01f, scale.y));
                rt.anchoredPosition = new Vector2(i % columns * across + rt.pivot.x * cell.x, -(i / columns * down + (1f - rt.pivot.y) * cell.y));
            }
            // The grid numbers its slots again and shows the new ones
            AccessTools.Method(typeof(InventoryGrid), "LoadSlots").Invoke(grid, null);
            AccessTools.Method(typeof(InventoryGrid), "UpdateInventory").Invoke(grid, null);

            ScrollRect scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false;
            scroll.scrollSensitivity = 0f;
            scroll.verticalScrollbar = Bar(panel, all);
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            view.gameObject.AddComponent<WheelScroll>().Init(scroll, down);
            scroll.verticalNormalizedPosition = 1f;
            Open = scroll;
        }

        /// <summary>Development: the grid, its parents and its first slots, to check the layout in the log.</summary>
        private static void Describe(RectTransform content)
        {
            for (Transform t = content; t != null && t.GetComponent<Canvas>() == null; t = t.parent)
            {
                var rt = t as RectTransform;
                string parts = string.Join(",", t.GetComponents<Component>().Select(c => c.GetType().Name));
                IndustriesMod.Log.Msg($"dev: menu {t.name} [{parts}] rect {rt?.rect} anchors {rt?.anchorMin}-{rt?.anchorMax} pos {rt?.anchoredPosition} size {rt?.sizeDelta} scale {t.localScale}");
            }
            for (int i = 0; i < content.childCount && i < 3; i++)
            {
                var c = (RectTransform)content.GetChild(i);
                IndustriesMod.Log.Msg($"dev: menu slot {c.name} at {c.localPosition} rect {c.rect} scale {c.localScale}");
            }
        }

        /// <summary>A slot's rectangle in the panel's space.</summary>
        private static Rect Box(RectTransform slot, RectTransform panel)
        {
            var corners = new Vector3[4];
            slot.GetWorldCorners(corners);
            Vector2 min = panel.InverseTransformPoint(corners[0]), max = min;
            foreach (Vector3 corner in corners)
            {
                Vector2 p = panel.InverseTransformPoint(corner);
                min = Vector2.Min(min, p);
                max = Vector2.Max(max, p);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>The scroll view: where the game's 10 slots were, hiding the others.</summary>
        private static RectTransform View(RectTransform panel, int index, Rect area)
        {
            var holder = new GameObject("SI_ContainerScroll", typeof(RectTransform));
            var view = (RectTransform)holder.transform;
            view.SetParent(panel, false);
            view.SetSiblingIndex(index);
            Place(view, area);
            holder.AddComponent<LayoutElement>().ignoreLayout = true;
            // A transparent image catches the pointer between the slots (drag and drop, wheel)
            holder.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            holder.AddComponent<RectMask2D>();
            return view;
        }

        /// <summary>Puts a rectangle at a place given in its parent's space.</summary>
        private static void Place(RectTransform rt, Rect area)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.localScale = Vector3.one;
            rt.sizeDelta = area.size;
            rt.localPosition = new Vector3(area.center.x, area.center.y, 0f);
        }

        /// <summary>A thin scroll bar in the panel's margin right of the slots (outside the view, whose mask would hide it).</summary>
        private static Scrollbar Bar(RectTransform panel, Rect slots)
        {
            float margin = Mathf.Max(8f, panel.rect.xMax - slots.xMax);
            float width = Mathf.Clamp(margin * 0.45f, 6f, 16f);
            var track = new GameObject("SI_ContainerScrollBar", typeof(RectTransform));
            var rt = (RectTransform)track.transform;
            rt.SetParent(panel, false);
            Place(rt, new Rect(slots.xMax + (margin - width) * 0.5f, slots.yMin, width, slots.height));
            track.AddComponent<LayoutElement>().ignoreLayout = true;
            track.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);

            var area = new GameObject("Sliding Area", typeof(RectTransform));
            var art = (RectTransform)area.transform;
            art.SetParent(rt, false);
            art.anchorMin = Vector2.zero;
            art.anchorMax = Vector2.one;
            art.sizeDelta = Vector2.zero;
            var handle = new GameObject("Handle", typeof(RectTransform));
            var hrt = (RectTransform)handle.transform;
            hrt.SetParent(art, false);
            hrt.sizeDelta = Vector2.zero;
            Image grip = handle.AddComponent<Image>();
            grip.color = new Color(1f, 0.6f, 0.15f, 0.9f);

            Scrollbar bar = track.AddComponent<Scrollbar>();
            bar.handleRect = hrt;
            bar.targetGraphic = grip;
            bar.direction = Scrollbar.Direction.BottomToTop;
            return bar;
        }

        /// <summary>
        /// The mouse wheel scrolls the view while the pointer is over it (the slots' event
        /// triggers take the scroll events, so the scroll view never gets them itself).
        /// </summary>
        private sealed class WheelScroll : MonoBehaviour
        {
            private ScrollRect _scroll;
            private float _row;

            public void Init(ScrollRect scroll, float row)
            {
                _scroll = scroll;
                _row = row;
            }

            private void Update()
            {
                Mouse mouse = Mouse.current;
                if (_scroll == null || mouse == null)
                    return;
                float wheel = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(wheel) < 0.01f)
                    return;
                Canvas canvas = GetComponentInParent<Canvas>();
                Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
                if (!RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, mouse.position.ReadValue(), camera))
                    return;
                ScrollBy(_scroll, wheel > 0f ? 1 : -1, _row);
            }
        }

        /// <summary>Scrolls by whole rows, up (positive) or down (negative).</summary>
        public static void ScrollBy(ScrollRect scroll, int rows, float row)
        {
            float hidden = scroll.content.rect.height - scroll.viewport.rect.height;
            if (hidden <= 0f)
                return;
            scroll.verticalNormalizedPosition = Mathf.Clamp01(scroll.verticalNormalizedPosition + rows * row / hidden);
        }
    }

    [HarmonyPatch(typeof(StorageMenuActivator), nameof(StorageMenuActivator.ShowMenu))]
    internal static class ContainerMenuPatch
    {
        private static readonly AccessTools.FieldRef<StorageMenuActivator, GameObject> CurrentMenu =
            AccessTools.FieldRefAccess<StorageMenuActivator, GameObject>("_currentMenu");

        private static void Postfix(StorageMenuActivator __instance)
        {
            try
            {
                GameObject menu = CurrentMenu(__instance);
                int count = ContainerMenu.OpenSlotCount();
                if (menu != null && count > 10)
                    ContainerMenu.Expand(menu, count);
            }
            catch (Exception e)
            {
                ErrorLog.Report("container menu", e);
            }
        }
    }
}
