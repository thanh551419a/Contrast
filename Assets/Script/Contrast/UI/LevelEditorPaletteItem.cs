using Contrast.Level;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Contrast.UI
{
    public sealed class LevelEditorPaletteItem : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private LevelMarkerType markerType = LevelMarkerType.End;
        [SerializeField] private bool isMarker;
        private LevelEditorController controller;

        public void Initialize(LevelEditorController editor, LevelMarkerType type, bool marker)
        {
            controller = editor;
            markerType = type;
            isMarker = marker;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            controller ??= FindAnyObjectByType<LevelEditorController>();
            controller?.BeginPaletteDrag(markerType, isMarker);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            controller ??= FindAnyObjectByType<LevelEditorController>();
            controller?.EndPaletteDrag(eventData.position);
        }
    }
}
