using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace AnatoLab
{
    public sealed class OrganSelector : MonoBehaviour
    {
        public Camera viewCamera;
        public OrbitCamera orbit;
        public Material ghostMaterial;
        public bool xrayEnabled = true;
        public OrganData Selected { get; private set; }
        public event Action<OrganData> SelectionChanged;
        public event Action<OrganData> HoverChanged;
        OrganData hover;
        sealed class Visual
        {
            public OrganData organ;
            public Renderer renderer;
            public Material[] original;
            public ShadowCastingMode shadows;
        }
        readonly List<Visual> visuals = new List<Visual>();
        MaterialPropertyBlock block;
        public void Initialize(OrganData[] organs)
        {
            block = new MaterialPropertyBlock();
            visuals.Clear();
            foreach (var organ in organs)
                foreach (var r in organ.GetComponentsInChildren<Renderer>())
                    visuals.Add(new Visual { organ = organ, renderer = r, original = r.sharedMaterials, shadows = r.shadowCastingMode });
            RefreshVisuals();
        }
        void Update()
        {
            if (viewCamera == null) return;
            bool blocked = (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) || (orbit != null && orbit.IsDragging);
            OrganData hitOrgan = null;
            if (!blocked)
            {
                var ray = viewCamera.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out var hit, 100f, 1 << 8, QueryTriggerInteraction.Ignore)) hitOrgan = hit.collider.GetComponentInParent<OrganData>();
            }
            if (hover != hitOrgan) { hover = hitOrgan; RefreshVisuals(); HoverChanged?.Invoke(hover); }
            if (!blocked && Input.GetMouseButtonDown(0) && hitOrgan != null) Select(hitOrgan, false);
        }
        public void Select(OrganData organ, bool focus = true)
        {
            if (organ == null) { ClosePanel(); return; }
            Selected = organ;
            RefreshVisuals();
            SelectionChanged?.Invoke(organ);
            if (focus && orbit != null) orbit.FocusOn(organ);
        }
        public void ClosePanel() { Selected = null; hover = null; RefreshVisuals(); SelectionChanged?.Invoke(null); HoverChanged?.Invoke(null); }
        public void ToggleXray() { xrayEnabled = !xrayEnabled; RefreshVisuals(); }
        public void RefreshVisuals()
        {
            if (block == null) block = new MaterialPropertyBlock();
            foreach (var v in visuals)
            {
                if (v.renderer == null) continue;
                bool ghost = xrayEnabled && Selected != null && v.organ != Selected;
                if (ghost && ghostMaterial != null)
                {
                    var mats = new Material[v.original.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = ghostMaterial;
                    v.renderer.sharedMaterials = mats;
                    v.renderer.shadowCastingMode = ShadowCastingMode.Off;
                }
                else { v.renderer.sharedMaterials = v.original; v.renderer.shadowCastingMode = v.shadows; }
                block.Clear();
                if (!ghost && (v.organ == Selected || v.organ == hover))
                {
                    bool selected = v.organ == Selected;
                    var tint = selected ? new Color(1f, 0.78f, 0.47f) : new Color(0.45f, 1f, 0.9f);
                    block.SetColor("_BaseColor", tint);
                    block.SetColor("_Color", tint);
                    block.SetColor("_EmissionColor", selected ? new Color(0.16f, 0.07f, 0.015f) : new Color(0, 0.15f, 0.1f));
                }
                v.renderer.SetPropertyBlock(block);
            }
        }
        void OnDisable()
        {
            foreach (var v in visuals) if (v.renderer != null) { v.renderer.sharedMaterials = v.original; v.renderer.SetPropertyBlock(null); v.renderer.shadowCastingMode = v.shadows; }
        }
    }
}

// AnatoLab package revision: 1795099051143225200
