import { HubConnectionBuilder } from "@microsoft/signalr";

class GameHubService {
  instance = null;
  hubConnection = null;

  constructor() {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl("/game")
      .withAutomaticReconnect()
      .build();

    this.hubConnection.start().catch((err) => {
      console.log(err);
    });
  }

  static getConnection() {
    if (this.instance == null) {
      this.instance = new GameHubService();
    }

    return this.instance.hubConnection;
  }
}

export default GameHubService;
