using UnityEngine;

public class HexArenaGenerator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int _layers = 5;
    [SerializeField] private float _tileSpacing = 1.0f;
    [SerializeField] private float _yPosition = 1f;
    [SerializeField] private GameObject _tilePrefab;
    [SerializeField] private Transform _container;

    [ContextMenu("Generate Hex Arena")]
    public void GenerateArena()
    {
        if (_container == null)
        {
            Debug.LogError("Container is null!");
            return;
        }

        // Clear existing tiles
        foreach (Transform child in _container)
        {
            DestroyImmediate(child.gameObject);
        }

        int tileIndex = 0;

        // Layer 0: Center (1 tile)
        CreateTile(0, 0, tileIndex++, "Center");
        // Layer 1: 6 tiles
        for (int i = 0; i < 6; i++)
        {
            float angle = i * 60f * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * _tileSpacing * 1f;
            float z = Mathf.Sin(angle) * _tileSpacing * 1f;
            CreateTile(x, z, tileIndex++, $"Layer1_{i}");
        }
        // Layer 2: 12 tiles
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * _tileSpacing * 2f;
            float z = Mathf.Sin(angle) * _tileSpacing * 2f;
            CreateTile(x, z, tileIndex++, $"Layer2_{i}");
        }
        // Layer 3: 18 tiles
        for (int i = 0; i < 18; i++)
        {
            float angle = i * 20f * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * _tileSpacing * 3f;
            float z = Mathf.Sin(angle) * _tileSpacing * 3f;
            CreateTile(x, z, tileIndex++, $"Layer3_{i}");
        }
        // Layer 4: 24 tiles
        for (int i = 0; i < 24; i++)
        {
            float angle = i * 15f * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * _tileSpacing * 4f;
            float z = Mathf.Sin(angle) * _tileSpacing * 4f;
            CreateTile(x, z, tileIndex++, $"Layer4_{i}");
        }

        Debug.Log($"Generated {tileIndex} hex tiles in {_layers} layers");
    }

    private void CreateTile(float x, float z, int index, string name)
    {
        GameObject tile;
        
        if (_tilePrefab != null)
        {
            tile = Instantiate(_tilePrefab, _container);
        }
        else
        {
            // Create cube if no prefab
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tile = cube;
            cube.transform.SetParent(_container);
            
            // Remove collider (HexTile has its own)
            Collider col = cube.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            
            // Set scale for hex tile shape
            cube.transform.localScale = new Vector3(1f, 0.2f, 1f);
            
            // Add HexTile script
            HexTile hexTile = cube.AddComponent<HexTile>();
        }

        tile.name = $"HexTile_{name}";
        tile.transform.localPosition = new Vector3(x, _yPosition, z);
        tile.transform.localRotation = Quaternion.identity;
    }
}
