import React, { useState, useEffect } from "react";
import GameHubService from "../../services/gameHubService";

export const GameRoom = ({ match }) => {
  const id = match.params.id;
  const [users, setUsers] = useState([]);
  const [signalR, setSignalR] = useState(GameHubService.getConnection());

  const [joined, setJoined] = useState(false);
  const [username, setUsername] = useState("");

  const [word, setWord] = useState("");
  const [answer, setAnswer] = useState("");

  const joinGame = () => {
    signalR.invoke("joinGame", username, id).then((success) => {
      if (success) {
        setJoined(true);
      } else {
        alert("Failed to join, game is full");
      }
    });
  };

  const leaveGame = () => {
    signalR.invoke("leaveGame", username, id).then(() => {
      setJoined(false);
      setUsername("");
    });
  };

  const viewWord = (index) => {
    signalR.invoke("viewWord", id, username, index);
  };

  const submitAnswer = () => {
    signalR
      .invoke("submitAnswer", id, username, answer)
      .then(() => setAnswer(""));
  };

  useEffect(() => {
    signalR.on("RefreshWord", (word) => {
      setWord(word);
    });

    signalR.on("PopulatePlayers", (players) => {
      setUsers(players);
    });

    signalR.on("UpdateRoomPlayerInfo", (players) => {
      setUsers(players);
    });

    signalR.on("ReceiveAnswer", (username, answer, correct) => {
      alert(
        `${username} said ${answer}, it was ${correct ? "correct" : " wrong"}!`
      );
    });

    signalR.invoke("JoinRoom", id);

    return () => {
      signalR.off("RefreshWord");
      signalR.off("PopulatePlayers");
      signalR.off("ReceiveAnswer");
      signalR.invoke("LeaveRoom", id);
    };
  }, [signalR]);

  useEffect(() => {
    if (signalR) {
      console.log("register joined");
      signalR.off("PlayerJoined");
      signalR.on("PlayerJoined", (user) => {
        const currentUsers = [...users];
        currentUsers.push({ ...user });
        setUsers(currentUsers);
      });

      signalR.off("PlayerLeaved");
      signalR.on("PlayerLeaved", (username) => {
        const currentUsers = users.filter((x) => x.username !== username);
        setUsers(currentUsers);
      });

      signalR.off("UpdatePlayer");
      signalR.on("UpdatePlayer", (username, money) => {
        const currentUsers = [...users];
        const index = currentUsers.findIndex((x) => x.username === username);
        currentUsers[index].money = money;
        setUsers(currentUsers);
      });
    }
  }, [signalR, users]);

  return (
    <div className="row">
      <div className="col-sm-3">
        {joined ? (
          <button type="button" onClick={leaveGame}>
            Leave Game
          </button>
        ) : (
          <>
            <h3>Enter Game</h3>
            <input
              type="text"
              placeholder="Username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
            />
            <button type="button" onClick={joinGame}>
              Submit
            </button>
          </>
        )}

        <div className="panel panel-default">
          <div className="panel-body">
            <h3>Players</h3>
            <ul className="users">
              {users.map((user, index) => (
                <li key={index}>
                  <span>{user.username}</span>
                  <span style={{ float: "right" }}>${user.money}</span>
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>

      {word != null && (
        <div className="col-sm-9">
          <div className="jumbotron">
            <div style={{ display: "flex", justifyContent: "center" }}>
              <div className="row">
                {word.split("").map((char, index) => (
                  <div key={index} style={{ margin: "10px" }}>
                    {char == "*" ? (
                      <button
                        onClick={joined ? () => viewWord(index) : undefined}
                      >
                        *
                      </button>
                    ) : (
                      <span>{char}</span>
                    )}
                  </div>
                ))}
              </div>
            </div>
          </div>
          {joined && word && (
            <div>
              <input
                type="text"
                value={answer}
                onChange={(e) => setAnswer(e.target.value)}
              />
              <button type="button" onClick={submitAnswer}>
                Submit
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
