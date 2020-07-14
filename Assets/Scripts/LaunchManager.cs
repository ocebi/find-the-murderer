using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LaunchManager : MonoBehaviourPunCallbacks
{

    public GameObject EnterGamePanel;
    public GameObject ConnectionStatusPanel;
    public GameObject LobbyPanel;
    public InputField PlayerNameInputField;

    #region Unity Methods
    // Start is called before the first frame update
    void Start()
    {
        EnterGamePanel.SetActive(true);
        ConnectionStatusPanel.SetActive(false);

    }

    private void Awake()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    #endregion

    #region Public Methods
    public void ConnectToPhotonServer()
    {
        if(string.IsNullOrEmpty(PlayerNameInputField.text))
        {
            PhotonNetwork.NickName = "User" + Random.Range(1, 1000);
        }

        if(!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();

            EnterGamePanel.SetActive(false);
            ConnectionStatusPanel.SetActive(true);
        }
    }

    public void JoinRandomRoom()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    #endregion


    #region Photon Callbacks

    public override void OnConnected()
    {
        print("Connected to internet.");
    }

    public override void OnConnectedToMaster()
    {
        print(PhotonNetwork.NickName + " Connected to server.");
        ConnectionStatusPanel.SetActive(false);
        LobbyPanel.SetActive(true);

    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        print("Disconnected from server for reason " + cause.ToString());
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        print(otherPlayer.NickName + " disconnected from the room.");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        CreateAndJoinRoom();
    }
    
    public override void OnJoinedRoom()
    {
        print(PhotonNetwork.NickName + " joined to " + PhotonNetwork.CurrentRoom.Name);
        PhotonNetwork.LoadLevel("GameScene");
    }
    
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        print(newPlayer.NickName + " joined " + PhotonNetwork.CurrentRoom.Name + " (" + PhotonNetwork.CurrentRoom.PlayerCount + "/20)");
    }
    
    #endregion

    #region Private Methods

    private void CreateAndJoinRoom()
    {
        string roomName = "Room" + Random.Range(0, 10000);
        RoomOptions roomOptions = new RoomOptions();
        roomOptions.IsOpen = true;
        roomOptions.IsVisible = true;
        roomOptions.MaxPlayers = 20;
        PhotonNetwork.CreateRoom(roomName, roomOptions);
    }

    #endregion
}
