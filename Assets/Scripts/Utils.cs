using UnityEngine;

public static class Utils {
    private static Camera _camera;
    public static Camera Camera {
        get {
            if (_camera == null) {
                _camera = UnityEngine.Camera.main;
            }
            return _camera;
        }
    }
}