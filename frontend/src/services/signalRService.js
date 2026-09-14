// src/services/signalRService.js
//
// Replaces socket.io-client (src/services.js used to export a bare
// `socket` object connected to the old Node server). RunHub is now
// authenticated (see TestVault.Web/Program.cs's JwtBearerEvents.OnMessageReceived
// and the removal of MapHub(...).AllowAnonymous()), so this - unlike the
// old socket.io usage - needs to supply the current access token.
import * as signalR from '@microsoft/signalr';
import { apiOrigin } from './apiClient';
import { getAccessToken } from './tokenStorage';

// Exact server-side event names - see TestVault.Web/Hubs/IRunHubClient.cs.
// Old Socket.IO name -> new SignalR method name:
//   run:started  -> RunStarted
//   run:log      -> RunLog
//   run:finished -> RunFinished
//   run:stopped  -> RunStopped
//   run:error    -> RunError
export const RUN_EVENTS = {
  RUN_STARTED: 'RunStarted',
  RUN_LOG: 'RunLog',
  RUN_FINISHED: 'RunFinished',
  RUN_STOPPED: 'RunStopped',
  RUN_ERROR: 'RunError',
};

let connection = null;

function getConnection() {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl(`${apiOrigin}/hubs/run`, {
        // Called fresh on every (re)connect attempt, not just once at
        // startup - so a token refreshed in the meantime (or issued at a
        // brand new login) is always the one actually sent. Browsers can't
        // set a custom Authorization header on the underlying
        // WebSocket/SSE transport, so the SignalR client instead appends
        // this as an ?access_token= query parameter - see
        // JwtBearerEvents.OnMessageReceived in Program.cs, which is the
        // server-side half of this same workaround.
        accessTokenFactory: () => getAccessToken() || '',
      })
      // Built-in reconnect with backoff - the rough equivalent of
      // Socket.IO's `reconnection: true` default.
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }

  return connection;
}

/** Subscribe to a hub event (see RUN_EVENTS). Safe to call before start() - SignalR queues handlers. */
export function on(eventName, handler) {
  getConnection().on(eventName, handler);
}

export function off(eventName, handler) {
  getConnection().off(eventName, handler);
}

/** Starts the connection if it isn't already connected/connecting. Idempotent - safe to call from multiple components' effects. */
export async function start() {
  const conn = getConnection();
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    await conn.start();
  }
  return conn;
}

export async function stop() {
  if (connection) {
    await connection.stop();
  }
}

/** Tears down the connection object entirely (not just stops it) so the next start() builds a fresh one with a fresh accessTokenFactory read - call on logout. */
export async function reset() {
  if (connection) {
    await connection.stop();
    connection = null;
  }
}
