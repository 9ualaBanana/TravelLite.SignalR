import React, { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

const App = () => {
    const [connection, setConnection] = useState(null);
    const [connectionStatus, setConnectionStatus] = useState('Disconnected'); // State for connection status
    const [data, setData] = useState([]); // State to hold incoming data

    const api = process.env.REACT_APP_SERVER_HOST;

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

    const connect = async () => {
        // Create a new SignalR connection
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(`${api}/huh`, { accessTokenFactory })
            .withAutomaticReconnect()
            .build();

        // Set up event handlers for connection state changes
        newConnection.onreconnecting(() => {
            console.log('Reconnecting...');
            setConnectionStatus('Reconnecting'); // Update status to Reconnecting
        });

        newConnection.onreconnected(() => {
            console.log('Reconnected!');
            setConnectionStatus('Connected'); // Update status to Connected
        });

        newConnection.onclose(() => {
            console.log('Connection closed.');
            setConnectionStatus('Disconnected'); // Update status to Disconnected
        });

        // Set up event handler for incoming data
        newConnection.on('data', (timestamp) => {
            console.log(timestamp);
            setData(prevData => [...prevData, timestamp]); // Append new data to the existing data array
        });

        try {
            await newConnection.start();
            console.log('Connected!');
            setConnectionStatus('Connected'); // Update status to Connected
            setConnection(newConnection); // Store the new connection
        } catch (e) {
            console.error('Connection failed: ', e);
            setConnectionStatus('Disconnected'); // Update status to Disconnected
        }
    };

    const stop = async () => {
        if (connection && connection.state === signalR.HubConnectionState.Connected) {
            await connection.stop();
            setConnectionStatus('Disconnected'); // Update status to Disconnected
            setConnection(null); // Clear the connection
        }
    };

    return (
        <div>
            <h1>SignalR</h1>
            <h2>Status: {connectionStatus}</h2>
            <button onClick={connect}>Subscribe</button>
            <button onClick={stop}>Unsubscribe</button>

            {/* Display incoming data */}
            <div>
                <h3>Data Received:</h3>
                <ul>
                    {data.map((item, index) => (
                        <li key={index}>{item}</li> // Render each item in a list
                    ))}
                </ul>
            </div>
        </div>
    );
};

export default App;
