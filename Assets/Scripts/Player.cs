using System;
using UnityEngine;
using System.Linq;
using Photon.Pun;
using UnityEngine.InputSystem;

public class PlayerCustomProperty
{
    public static string TargetFound = "TargetFound";
}

public class Player : MonoBehaviourPunCallbacks
{
    public Material[] Colors;
    private readonly Color[] _lineColors = new Color[] {
        new Color(0xC9 / 255f, 0x01 / 255f, 0x01 / 255f, 1),
        new Color(0xCC / 255f, 0xC8 / 255f, 0x00 / 255f, 1),
        new Color(0xCD / 255f, 0x02 / 255f, 0xB9 / 255f, 1),
    };
    public GameObject Model;
    public GameObject MassValue;
    public GameObject Distance;

    private PhotonView _photonView;
    private LineRenderer _lineRenderer;
    private Vector3 _size;
    private Vector3 _plateCenter;
    private bool _isBot;
    private Vector3 _rawInputMovement = Vector3.zero;

    private readonly float _playerSpeed = 0.05f;

    public void Awake()
    {
        _photonView = GetComponent<PhotonView>();
        _lineRenderer = GetComponentInChildren<LineRenderer>();
        _size = GetComponentInChildren<MeshRenderer>().bounds.size;
    }

    public void Update()
    {
        if (_plateCenter != null)
        {
            DrawLineToPlateCenter();
            DrawDistanceToPlateCenter();
        }

        if (_photonView.IsMine && !_isBot && !Model.GetComponent<Rigidbody>().isKinematic)
        {
            ProcessMovement();
        }
    }

    public void OnMovement(InputAction.CallbackContext value)
    {
        Vector2 inputMovement = value.ReadValue<Vector2>();
        _rawInputMovement = new Vector3(inputMovement.x, 0, inputMovement.y);
    }

    public override void OnPlayerPropertiesUpdate(Photon.Realtime.Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);

        if (changedProps.ContainsKey(PlayerCustomProperty.TargetFound))
        {
            bool playersFoundTarget = PhotonNetwork.PlayerList.All(GetPlayerTargetFound);
            SetActive(playersFoundTarget);
        }
    }

    [PunRPC]
    public void SetPlayerColorIndex(int colorIndex)
    {
        GetComponentInChildren<MeshRenderer>().material = Colors[colorIndex];
        _lineRenderer.startColor = _lineRenderer.endColor = _lineColors[colorIndex];
    }

    [PunRPC]
    public void SetPlayerMass(float value)
    {
        Model.GetComponent<Rigidbody>().mass = value;
        MassValue.GetComponent<TextMesh>().text = (value * 1000).ToString();
    }

    [PunRPC]
    public void SetPlateCenter(Vector3 value)
    {
        _plateCenter = value;
    }

    [PunRPC]
    public void SetIsBot(bool value)
    {
        _isBot = value;
    }

    private void SetActive(bool value)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = value;
        }
        Model.GetComponent<Rigidbody>().isKinematic = !value;
    }

    private bool GetPlayerTargetFound(Photon.Realtime.Player player)
    {
        return (bool)player.CustomProperties[PlayerCustomProperty.TargetFound];
    }

    private void DrawLineToPlateCenter()
    {
        _lineRenderer.SetPosition(0, _plateCenter);
        _lineRenderer.SetPosition(1, GetCenter());
    }

    private void DrawDistanceToPlateCenter()
    {
        Vector3 distanceLineCenter = Vector3.Lerp(GetCenter(), _plateCenter, 0.5f);
        Distance.transform.position = new Vector3(distanceLineCenter.x, distanceLineCenter.y + 0.01f, distanceLineCenter.z);
        Distance.GetComponentInChildren<TextMesh>().text = Math.Round(CalculateDistanceToPlateCenter() * 100, 0).ToString();
    }

    private double CalculateDistanceToPlateCenter()
    {
        return Vector3.Distance(GetCenter(), _plateCenter);
    }

    private Vector3 GetCenter()
    {
        return Model.transform.position + new Vector3(0, _size.y * 0.25f, 0);
    }

    private void ProcessMovement()
    {
        Vector3 velocity = _rawInputMovement;
        velocity = Camera.main.transform.TransformDirection(velocity);
        velocity.y = 0;
        if (velocity != Vector3.zero)
        {
            Quaternion directionRotation = Quaternion.LookRotation(velocity);
            Model.transform.rotation = new Quaternion(Model.transform.rotation.x, directionRotation.y, Model.transform.rotation.z, directionRotation.w);
        }
        Model.transform.Translate(velocity * _playerSpeed * Time.deltaTime, Space.World);
    }
}
