# dr.BmwData

A .NET library to access telemetry from [BMW Open Car Data](https://bmw-cardata.bmwgroup.com/thirdparty/public/home). Very much work in progress. 

Goals:
[] Demonstrate how to access BMW open car data
[] Poll and Store data from my own vehicle, somewhere
[] Possibly find ways of using this data in, for example, home assistant. Maybe as an MQTT sensor?

This project just started and is mainly scaffolding for now. It's my pet project and it's unlikely I will ever finish it into production quality. However if you find it useful, feel free to send PRs or open issues.
 
## Overview

This project consists of:
- **dr.BmwData**: A reusable class library containing the main implementation for accessing the BMW Open Car Data API.
- **dr.BmwData.Console**: A console application demonstrating the usage of the library and outputting telemetry data.
