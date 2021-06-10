using System;
using System.Collections;
using System.IO;
using System.Linq;
using Photon.Pun;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GameController : MonoBehaviourPunCallbacks
{
    public Material[] PlayerColors;
    public GameObject ImageTarget;
    public GameObject Plate;
    public GameObject Timer;
    public GameObject Hint;
    public GameObject GameOverWindow;
    public Text GameOverInfoMessage;
    public GameObject Stick;

    private PhotonView _photonView;
    private Text _timerValue;
    private Vector3 _plateCenter;
    private float _plateRadius;
    private GameObject _playerGameObject;
    private float _balancedSeconds = 0;
    private float _secondsPassed = 0;
    private int _integerSecondsPassed = 0;
    private bool _gameOver = false;

    private const int _goodTimeLimit = 60;
    private const int _normalTimeLimit = 90;

    private readonly Color _timerGoodColor = new Color32(0, 149, 39, 255);
    private readonly Color _timerNormalColor = new Color32(174, 125, 0, 255);
    private readonly Color _timerBadColor = new Color32(220, 39, 41, 255);


    public void Awake()
    {
        Application.targetFrameRate = 30;
        _photonView = GetComponent<PhotonView>();
        _timerValue = Timer.GetComponentInChildren<Text>();
        SetPlayerTargetFound(PhotonNetwork.LocalPlayer, false);
        _plateCenter = GetPlateCenter();
        _plateRadius = GetPlateRadius();
        _playerGameObject = InitializePlayer(Array.IndexOf(PhotonNetwork.PlayerList, PhotonNetwork.LocalPlayer), false);

        if (PhotonNetwork.IsMasterClient)
        {
            InitializePlayer(PhotonNetwork.PlayerList.Length, true);
        }
    }

    public GameObject InitializePlayer(int playerIndex, bool isBot)
    {
        GameObject gameObject = PhotonNetwork.Instantiate(Path.Combine("In game", "Bug"), GeneratePosition(), Quaternion.identity, 0);

        int playerColorIndex = playerIndex % PlayerColors.Length;

        PhotonView photonView = gameObject.GetComponent<PhotonView>();
        photonView.RPC("SetPlayerColorIndex", RpcTarget.All, (object)playerColorIndex);
        photonView.RPC("SetPlayerMass", RpcTarget.All, (object)GenerateMass());
        photonView.RPC("SetPlateCenter", RpcTarget.All, (object)_plateCenter);
        photonView.RPC("SetIsBot", RpcTarget.All, (object)isBot);

        return gameObject;
    }

    public void Update()
    {
        if (IsGameStarted() && !_gameOver)
        {
            Stick.SetActive(true);
            if (PhotonNetwork.IsMasterClient)
            {
                UpdateSecondsPassed();
                if (_secondsPassed > 3)
                {
                    CheckPlateAngle();
                }
            }
        }
        if (_gameOver)
        {
            GameOverWindow.SetActive(true);
            Stick.SetActive(false);
        }
    }

    public void ExitGame()
    {
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        if (!_gameOver)
        {
            _gameOver = true;
            GameOverInfoMessage.text = "2-ой игрок вышел из игры =(";
        }
    }

    public void OnTargetFound()
    {
        SetPlayerTargetFound(PhotonNetwork.LocalPlayer, true);
        Timer.SetActive(true);
        Hint.SetActive(false);
    }

    public void OnTargetLost()
    {
        SetPlayerTargetFound(PhotonNetwork.LocalPlayer, false);
        Timer.SetActive(false);
        Hint.SetActive(true);
    }

    [PunRPC]
    public void SetSecondsPassed(int secondsPassed)
    {
        _integerSecondsPassed = secondsPassed;
        _timerValue.text = FormatSecondsPassed(secondsPassed);
        UpdateTimerColor(secondsPassed);
    }

    [PunRPC]
    public void Win()
    {
        _gameOver = true;
        GameOverInfoMessage.text = $"{GetCongratulationMessage()}\n\nВы прошли игру за {FormatSecondsPassed(_integerSecondsPassed)}";
    }

    public void OnMovement(InputAction.CallbackContext value)
    {
        _playerGameObject.GetComponent<Player>().OnMovement(value);
    }

    private void UpdateTimerColor(int secondsPassed)
    {
        _timerValue.color = secondsPassed < _goodTimeLimit
            ? _timerGoodColor
            : secondsPassed < _normalTimeLimit
                ? _timerNormalColor
                : _timerBadColor;
    }

    private void SetPlayerTargetFound(Photon.Realtime.Player player, bool value)
    {
        ExitGames.Client.Photon.Hashtable hashtable = new ExitGames.Client.Photon.Hashtable();
        hashtable.Add(PlayerCustomProperty.TargetFound, value);
        player.SetCustomProperties(hashtable);
    }

    private Vector3 GetPlateCenter()
    {
        return Plate.GetComponent<MeshRenderer>().bounds.center + new Vector3(0, Plate.GetComponent<MeshRenderer>().bounds.size.y * 0.9f, 0);
    }

    private float GetPlateRadius()
    {
        return Plate.GetComponent<MeshCollider>().bounds.size.x * 0.5f;
    }

    private Vector3 GeneratePosition()
    {
        Vector2 randomCirclePoint = UnityEngine.Random.insideUnitCircle * (0.8f * _plateRadius);
        return new Vector3(
            randomCirclePoint.x,
            Plate.transform.position.y + 0.01f,
            randomCirclePoint.y
        );
    }

    private float GenerateMass()
    {
        return (float)Math.Round(UnityEngine.Random.Range(0.00004f, 0.00007f), 6);
    }

    private bool IsGameStarted()
    {
        return PhotonNetwork.PlayerList.All(GetPlayerTargetFound);
    }

    private bool GetPlayerTargetFound(Photon.Realtime.Player player)
    {
        return player.CustomProperties.ContainsKey(PlayerCustomProperty.TargetFound)
            && (bool)player.CustomProperties[PlayerCustomProperty.TargetFound];
    }

    private void CheckPlateAngle()
    {
        Vector3 plateRotation = Plate.transform.rotation.eulerAngles;
        if (CheckAngle(plateRotation.x) && CheckAngle(plateRotation.z))
        {
            if (_balancedSeconds == 0)
            {
                StartCoroutine(WaitUntilBalanced());
            }
            _balancedSeconds += Time.deltaTime;
        }
        else
        {
            _balancedSeconds = 0;
        }
    }

    private bool CheckAngle(float angle)
    {
        return angle < 4f || angle > 356f;
    }

    private IEnumerator WaitUntilBalanced()
    {
        yield return new WaitUntil(() => _balancedSeconds >= 3);
        StopCoroutine(WaitUntilBalanced());
        _balancedSeconds = 0;
        _photonView.RPC("Win", RpcTarget.All);
    }

    private string GetCongratulationMessage()
    {
        return _integerSecondsPassed < _goodTimeLimit
            ? "Отлично!"
            : _integerSecondsPassed < _normalTimeLimit
                ? "Хорошо!"
                : "Неплохо!";
    }

    private string FormatSecondsPassed(int secondsPassed)
    {
        return string.Format("{0:D2}:{1:D2}", secondsPassed / 60, secondsPassed % 60);
    }

    private void UpdateSecondsPassed()
    {
        _secondsPassed += Time.deltaTime;
        int newIntegerSecondsPassed = (int)Math.Floor(_secondsPassed);
        if (newIntegerSecondsPassed > _integerSecondsPassed)
        {
            _integerSecondsPassed = newIntegerSecondsPassed;
            _photonView.RPC("SetSecondsPassed", RpcTarget.All, (object)newIntegerSecondsPassed);
        }
    }
}
