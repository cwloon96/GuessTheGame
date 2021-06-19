import React from "react";
import { useHistory } from "react-router-dom";
import "./RoomContainer.css";

export const RoomContainer = ({ id, playerCount, spectatorCount }) => {
  const history = useHistory();

  return (
    <div onClick={(x) => history.push("/" + id)} className="room-container">
      <h6>{id}</h6>
      <div>Players: {playerCount}</div>
      <div>Spectators: {spectatorCount}</div>
    </div>
  );
};
