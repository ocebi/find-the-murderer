using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.SceneManagement;

public class MainGameManager : MonoBehaviourPunCallbacks
{
    public GameObject displayPlayersPanel;
    public GameObject gamePanel;
    public PhotonView photonView;
    public Text playersVotedText;
    public Text totalPlayersText;
    public Text startTimerText;
    public Text playerRoleText;
    public Text countDownText;
    public Text gameInfoText;
    public Text gameStageText;
    public ArrayList votedToStart = new ArrayList(); //who voted to start the game at the beginning
    public ArrayList activePlayers = new ArrayList(); //who is allowed to vote, alive
    public ArrayList votedToKill; //only local user exist in the array. might be improved later to see who else voted.
    public ArrayList voteButtons = new ArrayList(); //not actually is a button but a prefab
    public Dictionary<string, int> playerVoteCount; //how many votes each player has
    public bool startTimer = false;
    public string playerRole;
    int stage = 1;
    int day = 1;

    public GameObject selectPlayerButtonPrefab;

    private string murdererName;

    #region Unity Methods
    // Start is called before the first frame update
    void Start()
    {
        displayPlayersPanel.SetActive(true);
        gamePanel.SetActive(false);

        if(PhotonNetwork.IsConnected)
        {
            string playerList = "";
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                //playerList = displayPlayersPanel.GetComponentInChildren<Text>().text;
                //print(playerList);
                playerList += "\n" + p.NickName.ToString();
            }
            print(playerList);
            displayPlayersPanel.GetComponentInChildren<Text>().text = playerList;
            totalPlayersText.text = "/ " + PhotonNetwork.CurrentRoom.PlayerCount.ToString();

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    #endregion

    #region Public Methods

    public void CheckVotes()
    {
        // comment out for debug purposes
        if((int.Parse(playersVotedText.text) / PhotonNetwork.CurrentRoom.PlayerCount > 0.5f) && PhotonNetwork.CurrentRoom.PlayerCount >= 3)
        {
            //displayPlayersPanel.GetComponentInChildren<Text>().text = "Game Started";
            PhotonNetwork.CurrentRoom.IsOpen = false;
            StartGame();
        }
        
        //StartGame();
    }

    public void ButtonClicked()
    {
        bool hasVotedToStart = false;

        foreach(string s in votedToStart)
        {
            if(s == PhotonNetwork.NickName)
            {
                hasVotedToStart = true;
                break;
            }
        }

        if(!hasVotedToStart)
        {
            photonView.RPC("VoteToStart", RpcTarget.AllBuffered, PhotonNetwork.NickName);
            hasVotedToStart = true;
        }
        
    }


    public void StartGame()
    {
        if(PhotonNetwork.IsMasterClient)
        {
            int randomInt = Random.Range(1, PhotonNetwork.CurrentRoom.PlayerCount);
            print(randomInt);
            foreach(Player p in PhotonNetwork.PlayerList)
            {
                --randomInt;
                if(randomInt == 0)
                {
                    photonView.RPC("SendRoles", RpcTarget.All, p.NickName);
                    break;
                }
            }
            //photonView.RPC("SendRoles", RpcTarget.All, randomInt.ToString());

        }
    }

    public IEnumerator StartTimer(string s, int time)
    {
        if(s == "pregame")
        {
            if (time == 0)
            {
                playerRoleText.text = "Your role is: " + playerRole;
                startTimerText.gameObject.SetActive(false);
                displayPlayersPanel.SetActive(false);
                gamePanel.SetActive(true);
                MakePlayerButtons();
            }
            else
            {
                startTimerText.text = "Starting game in " + time;
                --time;
                //coroutine = StartCoroutine(StartTimer(s, time));
                //yield return coroutine;
                //yield return StartCoroutine(StartTimer(s, time));
                yield return new WaitForSeconds(1f);
                StartCoroutine(StartTimer(s, time));
            }
            
        }
        
        else if(s == "voteCountdown")
        {
            if(time == 0)
            {
                countDownText.gameObject.SetActive(false);
                CalculateMostVotes();
            }
            else
            {
                countDownText.text = time.ToString();
                --time;
                yield return new WaitForSeconds(1f);
                StartCoroutine(StartTimer(s, time));
            }
        }
        else if(s == "Murderer")
        {
            if (time == 0)
            {
                gameInfoText.text = "Murderer has won!";
                StartCoroutine(StartTimer("gameOver", 10));
            }
            else
            {
                --time;
                yield return new WaitForSeconds(1f);
                StartCoroutine(StartTimer(s, time));
            }
        }
        else if(s == "Villager")
        {
            if (time == 0)
            {
                gameInfoText.text = "Villagers have won!";
                StartCoroutine(StartTimer("gameOver", 10));
            }
            else
            {
                --time;
                yield return new WaitForSeconds(1f);
                StartCoroutine(StartTimer(s, time));
            }
        }
        else if(s == "gameOver")
        {
            if (time == 0)
            {
                GameOver();
            }
            else
            {
                --time;
                yield return new WaitForSeconds(1f);
                StartCoroutine(StartTimer(s, time));
            }
        }
        else if(s == "day")
        {
            if (time == 0)
            {
                gameStageText.text = "Day: " + day;
                MakePlayerButtons();
            }
            else
            {
                --time;
                yield return new WaitForSeconds(1f);
                StartCoroutine(StartTimer(s, time));
            }
        }
        else if (s == "night")
        {
            if (time == 0)
            {
                gameStageText.text = "Night: " + day;
                MakeKillButtons();
                ++day;
            }
            else
            {
                --time;
                yield return new WaitForSeconds(1f);
                StartCoroutine(StartTimer(s, time));
            }
        }

    }

    public void CalculateMostVotes()
    {
        if (voteButtons.Count > 0)
        {
            foreach (GameObject go in voteButtons)
            {
                Destroy(go);
            }
        }

        gameInfoText.gameObject.SetActive(true);

        List<KeyValuePair<string, int>> myList = playerVoteCount.ToList();

        myList.Sort(
            delegate (KeyValuePair<string, int> pair1,
            KeyValuePair<string, int> pair2)
            {
                return pair1.Value.CompareTo(pair2.Value);
            }
        );
        /*
        print("Printing pairs");
        foreach(KeyValuePair<string, int> pair in myList)
        {
            print(pair.Value);
        }
        */
        myList.Reverse();
        string killedPlayerRole = "Villager";
        if (myList[0].Value != myList[1].Value)
        {
            if(myList[0].Key == murdererName)
            {
                killedPlayerRole = "Murderer";
            }
            gameInfoText.text = myList[0].Key + " has been killed.\n" + myList[0].Key + "'s role was: " + killedPlayerRole;
            activePlayers.Remove(myList[0].Key); //remove killed player from active players
            if(myList[0].Key == PhotonNetwork.NickName)
            {
                playerRoleText.text = "You are dead.";
            }

            if (killedPlayerRole == "Murderer") //need a delay before gameover check
            {
                StartCoroutine(StartTimer("Villager",5));
            }
            else if(activePlayers.Count <= 2)
            {
                StartCoroutine(StartTimer("Murderer", 5));
            }
            //proceed to the next stage, murderer kills a player
            else
            {
                NextStage();
            }
            
        }
        else
        {
            gameInfoText.text = "Nobody was killed.";
            //proceed to the next stage, murderer kills a player
            NextStage();
        }
        if(voteButtons.Count > 0)
        {
            foreach (GameObject go in voteButtons)
            {
                Destroy(go);
            }
        }
        voteButtons.Clear();
        playerVoteCount.Clear();
        votedToKill.Clear();
        /*
        foreach(string s in activePlayers) //reset playerVoteCount manually since makekillbuttons run only on killer
        {
            playerVoteCount.Add(s, 0);
        }
        */
    }

    public void NextStage()
    {
        if (stage == 0)
        {
            StartCoroutine(StartTimer("day", 5)); //next stage is day
            stage = 1;
        }
        else if (stage == 1)
        {
            StartCoroutine(StartTimer("night", 5)); //next stage is night
            stage = 0;

            if (day == 15) //to block infinity
            {
                print("day: 15 - gameover");
                GameOver();
            }
        }
    }

    public void GameOver()
    {
        PhotonNetwork.AutomaticallySyncScene = false;
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.Disconnect(); //might be unnecessary
    }

    public void MakePlayerButtons()
    {
        playerVoteCount = new Dictionary<string, int>();
        votedToKill = new ArrayList();
        float buttonY = 280f;
        foreach(string s in activePlayers)
        {
            GameObject button = (GameObject)Instantiate(selectPlayerButtonPrefab); //dogru hizada instantiate et
            button.transform.SetParent(gamePanel.transform.GetChild(0),false); //world positions stays=false
            button.GetComponentInChildren<Button>().name = s;
            button.GetComponentInChildren<Button>().onClick.AddListener(OnClickedPlayer);
            button.transform.GetChild(1).GetComponent<Text>().text = s;
            button.transform.localPosition = new Vector3(0, buttonY, button.transform.position.z);
            voteButtons.Add(button); //isin bitince tum butonlari sil
            playerVoteCount.Add(s, 0);
            buttonY -= 110;
        }
        countDownText.gameObject.SetActive(true);
        StartCoroutine(StartTimer("voteCountdown", 10));
        //stage = 1;
        //countdown baslat
        //bitince en yuksek oylanan kisi activePlayerList'ten cikarilsin
        //killer olduyse oyun bitsin
        //day'i arttir
        //killer'in oldurmesi icin menuyu ac
    }

    public void MakeKillButtons()
    {
        if(PhotonNetwork.NickName == murdererName)
        {
            playerVoteCount = new Dictionary<string, int>();
            votedToKill = new ArrayList();
            float buttonY = 280f;
            foreach (string s in activePlayers)
            {
                if(s != PhotonNetwork.NickName)
                {
                    GameObject button = (GameObject)Instantiate(selectPlayerButtonPrefab); //dogru hizada instantiate et
                    button.transform.SetParent(gamePanel.transform.GetChild(0), false); //world positions stays=false
                    button.GetComponentInChildren<Button>().name = s;
                    button.GetComponentInChildren<Button>().onClick.AddListener(OnClickedVictim); //listener is different than playerbuttons
                    button.transform.GetChild(1).GetComponent<Text>().text = s;
                    button.transform.GetComponentInChildren<Button>().GetComponentInChildren<Text>().text = "Kill";
                    button.transform.localPosition = new Vector3(0, buttonY, button.transform.position.z);
                    voteButtons.Add(button); //isin bitince tum butonlari sil
                    playerVoteCount.Add(s, 0);
                    buttonY -= 110;
                }
                
            }
        }
        else
        {
            foreach(string s in activePlayers)
            {
                playerVoteCount.Add(s, 0);
            }
        }
        countDownText.gameObject.SetActive(true);
        StartCoroutine(StartTimer("voteCountdown", 10));
        //stage = 0;
    }

    public void OnClickedPlayer()
    {
        if(!votedToKill.Contains(PhotonNetwork.NickName) && activePlayers.Contains(PhotonNetwork.NickName))
        {
            string name = EventSystem.current.currentSelectedGameObject.name;
            photonView.RPC("VoteToKill", RpcTarget.All, name);
            votedToKill.Add(PhotonNetwork.NickName);
        }
    }

    public void OnClickedVictim()
    {
        if (!votedToKill.Contains(PhotonNetwork.NickName))
        {
            string name = EventSystem.current.currentSelectedGameObject.name;
            EventSystem.current.currentSelectedGameObject.GetComponent<Button>().GetComponent<Image>().color = Color.red;
            EventSystem.current.currentSelectedGameObject.GetComponent<Button>().GetComponentInChildren<Text>().color = Color.white;
            votedToKill.Add(PhotonNetwork.NickName);
            photonView.RPC("KilledByKiller", RpcTarget.All, name);
        }
    }

    #endregion

    #region RPC Calls

    [PunRPC]
    public void VoteToStart(string playerName)
    {

        playersVotedText.text = (int.Parse(playersVotedText.text) + 1).ToString();
        votedToStart.Add(playerName);
        CheckVotes();
    }

    [PunRPC]
    public void SendRoles(string s)
    {

        murdererName = s;
        if (s == PhotonNetwork.LocalPlayer.NickName)
        {
            playerRole = "Murderer";
        }
        else
        {
            playerRole = "Villager";

        }
        startTimerText.gameObject.SetActive(true);
        StartCoroutine(StartTimer("pregame", 5));

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            activePlayers.Add(p.NickName);
        }
    }

    [PunRPC]
    public void VoteToKill(string playerName)
    {
        playerVoteCount[playerName] = playerVoteCount[playerName] + 1;
        foreach (GameObject go in voteButtons)
        {
            Button b = go.GetComponentInChildren<Button>();
            if (b.name == playerName)
            {
                b.GetComponentInChildren<Text>().text = "Vote (" + playerVoteCount[playerName] + ")";
            }
        }
    }

    [PunRPC]
    public void KilledByKiller(string playerName)
    {
        playerVoteCount[playerName] = playerVoteCount[playerName] + 1;
    }

    #endregion



    #region Photon Callbacks
    public override void OnJoinedRoom()
    {
        print(PhotonNetwork.NickName + " joined to " + PhotonNetwork.CurrentRoom.Name);
        PhotonNetwork.LoadLevel("GameScene");
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene("GameLauncherScene");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        print(newPlayer.NickName + " joined " + PhotonNetwork.CurrentRoom.Name + " (" + PhotonNetwork.CurrentRoom.PlayerCount + "/20)");
        string playerList = displayPlayersPanel.GetComponentInChildren<Text>().text + "\n" + newPlayer.NickName;
        displayPlayersPanel.GetComponentInChildren<Text>().text = playerList;
        totalPlayersText.text = "/ " + PhotonNetwork.CurrentRoom.PlayerCount.ToString();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        string playerList = "";
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            playerList += "\n" + p.NickName.ToString();
        }
        print(playerList);
        displayPlayersPanel.GetComponentInChildren<Text>().text = playerList;
        totalPlayersText.text = "/ " + PhotonNetwork.CurrentRoom.PlayerCount.ToString();
        
        foreach(string s in votedToStart)
        {
            if(s == otherPlayer.NickName)
            {
                votedToStart.Remove(s);
                playersVotedText.text = (int.Parse(playersVotedText.text) - 1).ToString();
            }
        }
        CheckVotes();

        if(activePlayers.Contains(otherPlayer.NickName))
        {
            activePlayers.Remove(otherPlayer.NickName);
        }
    }
    #endregion
}
