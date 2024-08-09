import React, { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

const App = () => {
    const [connection, setConnection] = useState(null);

    const api = 'http://localhost:32769';
    useEffect(() => {
        // Function to fetch the access token
        const accessTokenFactory = async () => {
            try {
                const response = await fetch(`${api}/auth`);
                if (!response.ok) {
                    throw new Error(`HTTP error ${response.status}`);
                }
                return await response.text(); // Return the access token as plain text
            } catch (error) {
                console.error("Error fetching access token:", error);
                throw error;
            }
        };

        // Create a new SignalR connection
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${api}/huh`, { accessTokenFactory })
            .withAutomaticReconnect()
            .build();

        setConnection(newConnection);
    }, []);

    useEffect(() => {
        // Start the connection when it is established
        const startConnection = async () => {
            if (connection && connection.state === signalR.HubConnectionState.Disconnected) {
                try {
                    await connection.start();
                    console.log('Connected!');

                    // Set up event handlers
                    connection.on('data', (timestamp) => {
                        console.log(timestamp);
                    });
                } catch (e) {
                    console.error('Connection failed: ', e);
                }
            }
        };

        startConnection();
    }, [connection]);

    const connect = async () => {
        if (connection && connection.state === signalR.HubConnectionState.Disconnected) {
            await connection.start();
        }
    };

    const feed = async () => {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            await connection.invoke('Feed');
        }
    };

    const stop = async () => {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            await connection.stop();
        }
    };

    return (
        <div>
            <h1>SignalR</h1>
            <button onClick={connect}>Connect</button>
            <button onClick={stop}>Disconnect</button>
            <button onClick={feed}>Feed</button>
        </div>
    );
};

export default App;
