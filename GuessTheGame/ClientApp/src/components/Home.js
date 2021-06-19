import React, { useState, useEffect } from "react";
import { useHistory } from "react-router-dom";
import GameHubService from "../services/gameHubService";
import { RoomContainer } from "./home/RoomContainer";

export const Home = () => {
  const [signalR, setSignalR] = useState(GameHubService.getConnection());
  const [rooms, setRooms] = useState([]);
  const history = useHistory();

  useEffect(() => {
    signalR.invoke("retrieveRooms").then((res) => setRooms(res));

    signalR.on("updateRooms", (res) => {
      setRooms(res);
    });

    return () => {
      signalR.off("updateRooms");
    };
  }, []);

  const createGame = () => {
    signalR.invoke("createRoom").then((res) => history.push("/" + res));
  };

  return (
    <div>
      <button type="button" onClick={createGame}>
        Create Game
      </button>
      {rooms.length > 0 && (
        <div>
          <h1>Rooms</h1>
          <div style={{ width: "100%" }}>
            {rooms.map((room) => (
              <RoomContainer
                key={room.id}
                id={room.id}
                playerCount={room.playerCount}
                spectatorCount={room.spectatorCount}
              />
            ))}
          </div>
        </div>
      )}
    </div>
  );
};
