using UnityEngine;

namespace AnatoLab
{
    [DisallowMultipleComponent]
    public sealed class OrganData : MonoBehaviour
    {
        public string organName;
        public string latinName;
        public string region;
        [TextArea(5, 20)] public string description;
        [TextArea(2, 8)] public string function;
        public bool approximateRegion = true;
        public Vector3 viewDirection = Vector3.back;
        public Bounds Bounds
        {
            get
            {
                var renderers = GetComponentsInChildren<Renderer>();
                var b = new Bounds(transform.position, Vector3.one * 0.1f);
                if (renderers.Length > 0) { b = renderers[0].bounds; foreach (var r in renderers) b.Encapsulate(r.bounds); }
                return b;
            }
        }
        public string SearchText => (organName + " " + latinName + " " + region).ToLowerInvariant();
    }
}

// AnatoLab package revision: 1795099051142793400
