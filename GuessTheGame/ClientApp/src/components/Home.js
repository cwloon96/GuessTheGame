import React, { useState, useEffect } from "react";
import { HubConnectionBuilder } from "@microsoft/signalr";

export const Home = () => {
  const [users, setUsers] = useState([]);
  const [signalR, setSignalR] = useState(null);

  const [isLogin, setIsLogin] = useState(false);
  const [username, setUsername] = useState("");

  const [word, setWord] = useState("");
  const [answer, setAnswer] = useState("");

  const enterGame = () => {
    signalR.invoke("login", username).then((success) => {
      if (success) {
        setIsLogin(true);
      } else {
        alert("Failed to join, game is full");
      }
    });
  };

  const leaveGame = () => {
    signalR.invoke("logout", username).then(() => {
      setIsLogin(false);
      setUsername("");
    });
  };

  const viewWord = (index) => {
    signalR.invoke("viewWord", username, index);
  };

  const submitAnswer = () => {
    signalR.invoke("submitAnswer", username, answer).then(() => setAnswer(""));
  };

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl("/game")
      .withAutomaticReconnect()
      .build();

    connection.on("RefreshWord", (word) => {
      setWord(word);
    });

    connection.on("PopulatePlayers", (players) => {
      setUsers(players);
    });

    connection.on("ReceiveAnswer", (username, answer, correct) => {
      alert(
        `${username} said ${answer}, it was ${correct ? "correct" : " wrong"}!`
      );
    });

    setSignalR(connection);
  }, []);

  useEffect(() => {
    if (signalR) {
      if (signalR.connection.connectionState == "Disconnected") {
        signalR.start().catch((err) => {
          console.log(err);
        });
      }

      signalR.off("UserJoined");
      signalR.on("UserJoined", (user) => {
        const currentUsers = [...users];
        currentUsers.push({ ...user });
        setUsers(currentUsers);
      });

      signalR.off("UserLeaved");
      signalR.on("UserLeaved", (username) => {
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
        {isLogin ? (
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
            <button type="button" onClick={enterGame}>
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
                        onClick={isLogin ? () => viewWord(index) : undefined}
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
          {isLogin && word && (
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
