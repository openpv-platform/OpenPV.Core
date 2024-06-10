# OpenPV - Ahsoka.CS.Services

This project shows how to construct Ahsoka Services for use in your application.  

**Ahsoka Services**

An Ahsoka Service provides a way to allow your customer (or other projects) to interact with your application functionality in a way that... 

1. Privides good encapsulation of your functionality while exposing a single simple API.
2. Enables a Cross Platform set of messages (in the form of ProtoBuffer message objects) that can be consumed from numerious languages (C++, JS, etc.) while allowing you to implement
your functionality in your preferred technology.
3. Allows you to integrate your functionality in a variety of network infrastrastructures including consuming from the same process, consuming from a differenet process in the same machine, 
consuming your functionality isolated in a container such as Docker and finally consuming your functionality running on a separate machine boundry.


To accomplish these tasks, a service provider must construct 3 components that together implement the service.

1. Create the .proto definition file for the service.   This file describes the messages that you will send and recieve with your service.  Service can be implmented with Async or Sync 
messages which are defined in a ServiceMessage class.   Ahsoka Services use an Enumeration (in your proto) to describe the message types.  This enumeration becomes part of the message 
framing allowing the system to decode the various messages as they arrive at the Service or Client Endpoints
2. Create a Message Definition Class.  In this class, we will define the message structures that are sent to the service and the replies and notifications that the service can produce.
3. Create the Service Implementation.  This is where you implement the funtionality of your service.    Here you will primarily handle the reciept of messages, execute the work and then reply
to the Client.   Note that for more advanced scenarios such as supporting multiple platforms, there are utilities that allow you abstract the platform specific capabilities of yoru services.
This demo demostrates a simple example of separating Windows and Linux versions of our service.
4. Create the Client Impelemntation.    The client implementation is often the most simple part of your service in that it is mostly routing messages to your service based on how its configured
at startup however it also represents your changes to reshape or adapt your messages to your client.  For example, most clients adapt our fully async message  model to a blocking model for messages 
that can be quickly serviced.   However some clients in the Ahsoka Architecture may provide more advanced shaping such as transforming classes from type safe objects to general purpose objects using
reflection or simple construction patterns.


See the TimeService, TimeServiceClient and TimeServiceMessages for more info on each object.


&nbsp;

**[For more information about developing in OpenPV Click Here.](https://support.enovationcontrols.com/hc/en-us)**