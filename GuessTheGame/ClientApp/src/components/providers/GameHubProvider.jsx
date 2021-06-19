import React, { useState, useEffect } from "react";
import GameHubService from "../../services/gameHubService";

export const GameHubProvider = (props) => {
  const [initSuccess, setInitSuccess] = useState(undefined);

  const sleep = (ms) => {
    return new Promise((resolve) => setTimeout(resolve, ms));
  };

  useEffect(() => {
    async function init() {
      const signalR = GameHubService.getConnection();
      let sleepCount = 0;

      while (
        signalR.connection.connectionState !== "Connected" &&
        sleepCount < 5
      ) {
        await sleep(1000).then(sleepCount++);
      }

      setInitSuccess(signalR.connection.connectionState == "Connected");
    }

    init();
  }, []);

  return initSuccess !== undefined && (initSuccess ? props.children : <></>);
};
