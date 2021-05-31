using System.Collections.Generic;
using System.Linq;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    private readonly Color NORMAL_BUTTON_COLOR = new Color32(149, 91, 39, 255);
    private readonly Color DISABLED_BUTTON_COLOR = new Color32(48, 48, 48, 255);

    public GameObject[] Menus;
    public GameObject MainMenu;

    public GameObject[] DemoGameObjects;

    public LobbyManager LobbyManager;

    public InputField LobbyNameInputField;
    public Button CreateLobbyButton;

    public Text LobbyTitleText;
    public Text LobbyInfoText;
    public Button StartGameButton;

    public GameObject LobbiesParent;
    public GameObject LobbyListItemPrefab;
    public GameObject LobbyListItemDisabledPrefab;
    public GameObject LobbyMenu;

    private readonly List<RoomInfo> _roomInfoList = new List<RoomInfo>();


    public void Awake()
    {
        ShowMenu(Menus[0]);

        RemoveDemoGameObjects();

        LobbyManager.OnReady = OnReady;
        LobbyManager.OnLobbyListUpdate = OnLobbyListUpdate;
        LobbyManager.OnJoinedLobby = OnJoinedLobby;
        LobbyManager.OnPlayerJoinedLobby = OnPlayerJoinedLobby;
        LobbyManager.OnPlayerLeftLobby = OnPlayerLeftLobby;
        LobbyManager.Initialize();
    }

    public void ShowMenu(GameObject menuToShow)
    {
        foreach (GameObject menu in Menus)
        {
            menu.SetActive(false);
        }
        menuToShow.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void LeaveLobby()
    {
        LobbyManager.LeaveLobby();

        Text startGameButtonText = StartGameButton.GetComponentInChildren<Text>();
        startGameButtonText.color = DISABLED_BUTTON_COLOR;
        StartGameButton.interactable = false;
        StartGameButton.gameObject.SetActive(false);
    }

    public void OnLobbyNameInputChanged()
    {
        bool isLobbyNameEmpty = LobbyNameInputField.text.Length == 0;

        CreateLobbyButton.interactable = !isLobbyNameEmpty;

        Text createLobbyButtonText = CreateLobbyButton.GetComponentInChildren<Text>();
        createLobbyButtonText.color = isLobbyNameEmpty ? DISABLED_BUTTON_COLOR : NORMAL_BUTTON_COLOR;

        LobbyNameInputField.onEndEdit.RemoveAllListeners();
        if (!isLobbyNameEmpty)
        {
            LobbyNameInputField.onEndEdit.AddListener((string _) =>
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    CreateLobby(LobbyNameInputField);
                    ShowMenu(LobbyMenu);
                }
            });
        }
    }

    public void CreateLobby(InputField inputField)
    {
        LobbyManager.CreateLobby(inputField.text);
        LobbyTitleText.text = $"ИГРОВАЯ КОМНАТА «{inputField.text}»";
        LobbyInfoText.text = "Ожидайте подключения \n2-го игрока";
        StartGameButton.gameObject.SetActive(true);
    }

    public void StartGame()
    {
        LobbyManager.StartGame();
    }

    private void OnReady()
    {
        ShowMenu(MainMenu);
    }

    private void OnLobbyListUpdate(List<RoomInfo> newRoomInfoList)
    {
        IEnumerable<string> roomInfoNames = _roomInfoList.Select((RoomInfo roomInfo) => roomInfo.Name);
        _roomInfoList.AddRange(
            newRoomInfoList.Where(
                (RoomInfo newRoomInfo) =>
                    !newRoomInfo.RemovedFromList
                    && !roomInfoNames.Contains(newRoomInfo.Name)
            )
        );

        IEnumerable<string> removedRoomNames = newRoomInfoList
            .Where((RoomInfo roomInfo) => roomInfo.RemovedFromList || roomInfo.PlayerCount == 0)
            .Select((RoomInfo roomInfo) => roomInfo.Name);
        _roomInfoList.RemoveAll((RoomInfo roomInfo) => removedRoomNames.Contains(roomInfo.Name));

        UpdateLobbyListItems(_roomInfoList);
    }

    private void UpdateLobbyListItems(List<RoomInfo> roomList)
    {
        DestroyChildren(LobbiesParent);
        for (int i = 0; i < roomList.Count; ++i)
        {
            RoomInfo roomInfo = roomList[i];
            CreateLobbyListItem(roomInfo, i);
        }
        float paddingBottom = 2;
        float maxLobbyListItemOffsetY = GetMaxChildOffsetY(LobbiesParent);
        RectTransform lobbiesParentRect = LobbiesParent.GetComponent<RectTransform>();
        lobbiesParentRect.sizeDelta = new Vector2(
            lobbiesParentRect.sizeDelta.x,
            maxLobbyListItemOffsetY + paddingBottom);
    }

    private void CreateLobbyListItem(RoomInfo roomInfo, int index)
    {
        GameObject lobbyListItem = Instantiate(
            roomInfo.PlayerCount < roomInfo.MaxPlayers ? LobbyListItemPrefab : LobbyListItemDisabledPrefab,
            LobbiesParent.transform);

        Text lobbyListItemText = lobbyListItem.GetComponentInChildren<Text>();
        lobbyListItemText.text = roomInfo.Name;

        float paddingTop = 4;
        float spacingVertical = 16;
        RectTransform rectTransform = lobbyListItem.GetComponent<RectTransform>();
        rectTransform.localPosition = new Vector3(
            0,
            -paddingTop - (rectTransform.rect.height + spacingVertical) * index,
            0);

        if (roomInfo.PlayerCount < roomInfo.MaxPlayers)
        {
            Button button = lobbyListItem.GetComponent<Button>();
            button.onClick.AddListener(() => OnLobbyListItemClick(roomInfo));
        }
    }

    private float GetMaxChildOffsetY(GameObject gameObject)
    {
        float maxOffsetY = float.MinValue;
        foreach (Transform child in gameObject.transform)
        {
            RectTransform childRect = child.GetComponent<RectTransform>();
            maxOffsetY = Mathf.Max(maxOffsetY, Mathf.Abs(child.localPosition.y) + childRect.rect.height);
        }
        return maxOffsetY;
    }

    private void OnLobbyListItemClick(RoomInfo roomInfo)
    {
        LobbyTitleText.text = $"ИГРОВАЯ КОМНАТА «{roomInfo.Name}»";
        LobbyManager.JoinLobby(roomInfo.Name);
        ShowMenu(LobbyMenu);
    }

    private void RemoveDemoGameObjects()
    {
        foreach (GameObject demoGameObject in DemoGameObjects)
        {
            Destroy(demoGameObject);
        }
    }

    private void OnJoinedLobby(Room room)
    {
        if (room.PlayerCount == room.MaxPlayers)
        {
            LobbyInfoText.text = "Ожидайте начала игры";
        }
    }

    private void OnPlayerJoinedLobby(Room room)
    {
        if (room.PlayerCount == room.MaxPlayers)
        {
            LobbyInfoText.text = "2-ой игрок подключился";
            Text startGameButtonText = StartGameButton.GetComponentInChildren<Text>();
            startGameButtonText.color = NORMAL_BUTTON_COLOR;
            StartGameButton.interactable = true;
        }
    }

    private void OnPlayerLeftLobby(Room room)
    {
        LobbyInfoText.text = "Ожидайте подключения \n2-го игрока";
        if (room.PlayerCount == 1)
        {
            StartGameButton.gameObject.SetActive(true);
            Text startGameButtonText = StartGameButton.GetComponentInChildren<Text>();
            startGameButtonText.color = DISABLED_BUTTON_COLOR;
            StartGameButton.interactable = false;
        }
    }

    private void DestroyChildren(GameObject gameObject)
    {
        foreach (Transform child in gameObject.transform)
        {
            Destroy(child.gameObject);
        }
    }
}
