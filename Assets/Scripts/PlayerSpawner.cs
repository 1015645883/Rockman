using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("角色预制体")]
    public GameObject rockmanPrefab;
    public GameObject bluesPrefab;
    public GameObject cutmanPrefab;
    public GameObject gutsmanPrefab;
    public GameObject icemanPrefab;
    public GameObject bombmanPrefab;
    public GameObject firemanPrefab;
    public GameObject elecmanPrefab;

    [Header("出生点")]
    public Transform spawnPoint;

    [Header("主摄像机跟随脚本")]
    public Camera mainCamera; // 在 Inspector 里拖 MainCamera

    private void Start()
    {
        SpawnSelectedCharacter();
    }

    private void SpawnSelectedCharacter()
    {
        string selectedCharacter = PlayerPrefs.GetString("SelectedCharacter", "Rockman");
        GameObject prefabToSpawn = null;

        switch (selectedCharacter)
        {
            case "Rockman": prefabToSpawn = rockmanPrefab; break;
            case "Blues": prefabToSpawn = bluesPrefab; break;
            case "Cutman": prefabToSpawn = cutmanPrefab; break;
            case "Gutsman": prefabToSpawn = gutsmanPrefab; break;
            case "Iceman": prefabToSpawn = icemanPrefab; break;
            case "Bombman": prefabToSpawn = bombmanPrefab; break;
            case "Fireman": prefabToSpawn = firemanPrefab; break;
            case "Elecman": prefabToSpawn = elecmanPrefab; break;
        }

        if (prefabToSpawn != null && spawnPoint != null)
        {
            GameObject player = Instantiate(prefabToSpawn, spawnPoint.position, spawnPoint.rotation);

            // 摄像机绑定到玩家
            if (mainCamera != null)
            {
                StageCameraController cameraController =
                    mainCamera.GetComponent<StageCameraController>();

                if (cameraController != null)
                {
                    cameraController.SetTarget(
                        player.transform,
                        true
                    );
                }
                else
                {
                    // 没有StageCameraController时保留兼容
                    mainCamera.transform.position =
                        new Vector3(
                            player.transform.position.x,
                            player.transform.position.y,
                            -10f
                        );
                }
            }
        }
        else
        {
            Debug.LogWarning("未找到角色预制体或出生点，请检查绑定！");
        }
    }
}
