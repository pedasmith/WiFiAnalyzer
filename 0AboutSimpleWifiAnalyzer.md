# About Simple Wi-Fi Analyzer (WiFi)

This app is demonstrates the kind of information available using the UWP (WinRT) Wi-Fi API set

## Issues with the API

There is a **weird thing with the documentation**. The original API was all about tethering driven by a mobile operator. That is,if your laptop (or Windows phone!) has a cell connection, your cell carrier might have a deal with a Wi-Fi service (like Boingo) to get you Wi-Fi for free. But then the API was opened up to allow any app have Windows serve as an access point

But that makes the docs hard to discover. The docs are all squirreled away in a place that strongly implies that they are only intended for mobile operators, not for everyone.

There are **no events** on the manager. Desireable events would include events on the state (on/off etc.) and the number of connected clients. One reason to use the hotspot API is to allow smart devices to connect. The app that creates the hotspot would presumably like to know when the smart device connects and disconnects. Since the user is able to turn the mobile hotspot on and off, the app would presumably also like to know that.


There is **no client data** for the amount of data or the data connection speed. This is data that an app for a smart device would presumably like to know.

