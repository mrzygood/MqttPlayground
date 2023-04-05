### History and grasp of general information
The MQTT protocol was invented in 1999 for use in the oil and gas industry (for SCADA). 
Engineers needed a protocol for minimal bandwidth and minimal battery loss to monitor oil pipelines via satellite.
Today is the leading open source protocol for connecting internet of things.
The protocol has applications in industries ranging from automotive to energy to telecommunications.
It must run over a transport protocol that provides ordered, lossless, bi-directional connections—typically, TCP/IP[3].  
Exists MQTT-SN which is used over other transports such as UDP or Bluetooth.

Main advantages:
* Lightweight. Minimal MQTT control message can be as little as two data bytes.
* Allow connection many devices over unreliable and cellular networks with low bandwidth and high latency.

Fun fact:
MQTT is used in Facebook Messenger.

### Architecture
Works on the principles of the publish/subscribe (pub/sub) model (publisher=>broker=>subscriber) which is an alternative to traditional client-server architecture.

Main advantages of this architecture:
* Space decoupling.
  The publisher and subscriber are not aware of each other’s network location and do not exchange information such as IP addresses or port numbers.
* Time decoupling.
  The publisher and subscriber don’t run or have network connectivity at the same time.
* Synchronization decoupling.
  Both publishers and subscribers can send or receive messages without interrupting each other. For example, the subscriber does not have to wait for the publisher to send a message.

If a broker receives a message on a topic for which there are no current subscribers, the broker discards the message 
unless the publisher of the message designated the message as a retained message (retained flag set to true)[3].

### Reliability levels:
* (0) At most once - fire and forget.
* (1) At least once - sends message until receive acknowledge.
* (2) exactly once - two-level handshake*.

### Security
It has a minimal authentication feature built in. Username and password are sent as clear text. To make it secure, Secure Sockets Layer (SSL)/ Transport Layer Security (TLS) must be employed, but SSL/TLS is not a lightweight protocol.
The client may also provide a client certificate to the broker during the handshake. The broker can use this to authenticate the client.
While not specifically part of the MQTT specification, it has become customary for brokers to support client authentication with SSL/TLS client-side certificates.

Security of the MQTT protocol was compromised in 2020 by Italian researchers, executing Slow DoS Attacks on such protocol[3].

#### Connection

Both the MQTT client and the broker require a TCP/IP stack to communicate.
Clients never connect with each other, only with the broker.

The standard ports are 1883 for non-encrypted communication and 8883 for encrypted communication - using SSL/TLS.
During the SSL/TLS handshake, the client validates the server certificate and authenticates the server.
The client may also provide a client certificate to the broker during the handshake.

The broker keeps track of all the session's information as the device goes on and off, in a function called "persistent sessions"[3].

##### Persistent session
A client can request a persistent session when it connects to the broker.
Persistent sessions save all information that is relevant for the client on the broker.

The broker stores the following information:
* Existence of a session (even if there are no subscriptions)
* All the subscriptions of the client.
* All messages in a Quality of Service (QoS) 1 or 2 flow that the client has not yet confirmed.
* All new QoS 1 or 2 messages that the client missed while offline.
* All QoS 2 messages received from the client that are not yet completely acknowledged.

The clientId that the client provides when it establishes connection to the broker identifies the session.
Usually, the memory limit of the operating system is the primary constraint on message storage[5].

#### Messages
Each message consists of a fixed header - 2 bytes - an optional variable header, a message payload that is limited to 256 megabytes (MB) of information and a quality of service (QoS) level.
The format of the content will be application-specific.

###### Retained messages
When new subscriber appear it may not receive any message for long time if messages are publish rarely.
But you can mark messages as retained. Retained message is stored and sends to every to subscriber.
The broker stores only one retained message per topic.
There is also a very simple way to delete the retained message of a topic: send a retained message with a zero-byte payload on the topic where you want to delete the previous retained message.

##### Topic
The term "topic" refers to keywords the MQTT broker uses to filter messages for the MQTT clients.
Topics are organized hierarchically, similar to a file or folder directory with the use of a special delimiter character, the forward slash (/).

###### Special wildcard characters
* `+` - a single-level wildcard character.
* `#` - must be placed as the last character in the topic and preceded by a forward slash.  
  Note: You can specify multi-level wildcard as a topic (#) but it means you receive all messages that are sent to the MQTT broker.
  It may decrease efficiency.
* `$` - special topic character. Is reserved for internal statistics of broker. Clients cannot publish there.

The client does not need to create the desired topic before they publish or subscribe to it.
The broker accepts each valid topic without any prior initialization.

Example topic:`robot/wheel/velocity`
* `robot, wheel, velocity` - topic levels
* `/` - topic level separator

Anyone who subscribes to specific topic receives a copy of all messages for that topic[2].

###### Best practices
* Never use a leading forward slash.
  The leading forward slash introduces an unnecessary topic level with a zero character at the front.
  The zero does not provide any benefit and often leads to confusion.
* Never use spaces in a topic.
  When things are not going the way they should, spaces make it much harder to read and debug topics.
* Keep the MQTT topic short and concise. When it comes to small devices, every byte counts and topic length has a big impact.
* Use only ASCII characters, avoid non printable characters.
* Embed a unique identifier or the Client Id into the topic.
* Don’t subscribe to `#`.
* Use specific topics, not general ones.
  When you name topics, don’t use them in the same way as in a queue.
  Differentiate your topics as much as possible.
* Don’t forget extensibility. Think about future use-cases to allow yourself extend existing topics easily[4].

##### Ping
This packet sequence roughly translates to ARE YOU ALIVE/YES I AM ALIVE.
This operation has no other function than to maintain a live connection and ensure the TCP connection has not been shut down by a gateway or router.

##### Disconnect
When a publisher or subscriber wants to terminate an MQTT session, it sends a DISCONNECT message to the broker and then closes the connection.
This is called a graceful shutdown because it gives the client the ability to easily reconnect by providing its client identity and resuming where it left off.

###### Last will
Disconnect can happen suddenly without time for a publisher to send a DISCONNECT message. The broker may send subscribers a message from the publisher that the broker has previously cached.
The message, which is called a last will and testament, provides subscribers with instructions for what to do if the publisher dies unexpectedly[2].

#### Vhosts

MQTT does not support vhosts natively. 
Vhosts are a feature of AMQP (Advanced Message Queuing Protocol), which is a different 
messaging protocol that operates differently from MQTT.
Some MQTT brokers may offer a similar concept as vhosts through the use of topics or other configuration options.
For example, the Mosquitto MQTT broker allows you to create separate "instances" using a feature called listener.
Each instance can be configured to use a different set of authentication, access control, and other settings, effectively providing a form of vhosting.

#### MQTT plugin for RabbitMQ

Supported MQTT 3.1.1 features
* QoS0 and QoS1 publish & consume 
* QoS2 publish (downgraded to QoS1)
* Last Will and Testament (LWT)
* TLS 
* Session stickiness 
* Retained messages with pluggable storage backends

MQTT uses slashes ("/") for topic segment separators and AMQP 0-9-1 uses dots.
The plugin translates patterns under the hood to bridge the two.
This has one important limitation: MQTT topics that have dots in them won't work as expected 
and are to be avoided, the same goes for AMQP 0-9-1 routing keys that contains slashes.

Retained messages can be stored in RAM or on disk (max 2BG per vhost).

##### Connecting to other vhosts
Virtual host name can be passed as part of username (separated by colon): `my-vhost:mqtt-john`.
It means you are connecting to `my-vhost` with username `mqtt-john`.

More details about RabbitMQ with MQTT plugin: https://www.rabbitmq.com/mqtt.html

#### RabbitMQ vs MQTT
RabbitMQ and MQTT are both popular message brokers that are widely used in modern distributed systems.
However, they have some significant differences that distinguish them from each other.

* Protocol: RabbitMQ uses the Advanced Message Queuing Protocol (AMQP), while MQTT uses the MQTT protocol.
AMQP is a more complex and feature-rich protocol that provides more advanced messaging features than MQTT. MQTT, on the other hand, is a lightweight protocol designed for low-bandwidth, high-latency networks.
* Message delivery: RabbitMQ guarantees that messages will be delivered in the order they were sent, while MQTT does not.
RabbitMQ also supports different message delivery patterns, such as publish/subscribe and point-to-point, while MQTT only supports the publish/subscribe pattern.
* Scalability: RabbitMQ has built-in support for clustering and can scale horizontally, while MQTT
requires additional middleware to achieve high scalability.
* Persistence: RabbitMQ supports both in-memory and disk-based message
storage, while MQTT does not provide built-in persistence.
* Complexity: RabbitMQ is a more complex message broker and requires more
configuration and management than MQTT, which is simpler and easier to use.

## More

#### What is MQTT over WSS?
MQTT over WebSockets (WSS) is an MQTT implementation to receive data directly into a web browser.
The MQTT protocol defines a JavaScript client to provide WSS support for browsers.
In this case, the protocol works as usual but it adds additional headers to the MQTT messages to also support the WSS protocol.
You can think of it as the MQTT message payload wrapped in a WSS envelope.

#### Version 5.0

In 2019, OASIS released the official MQTT 5.0 standard. Version 5.0 includes the following major new features:

* Reason codes: Acknowledgements now support return codes, which provide a reason for a failure.
* Shared subscriptions: Allow the load to be balanced across clients and thus reduce the risk of load problems.
* Message expiry: Messages can include an expiry date and are deleted if they are not delivered within this time period.
* Topic alias: The name of a topic can be replaced with a single number[3].

#### Clouds vs MQTT
TODO

#### MQTTnet

#### Tools
* Multimeter: https://github.com/chkr1011/mqttMultimeter

 TODO:
More about https://github.com/dotnet/MQTTnet

#### WARNING!
Not thread-safe implementation.

Sources:  
[1] https://aws.amazon.com/what-is/mqtt/  
[2] https://www.techtarget.com/iotagenda/definition/MQTT-MQ-Telemetry-Transport  
[3] https://en.wikipedia.org/wiki/MQTT  
[4] https://www.hivemq.com/blog/mqtt-essentials-part-5-mqtt-topics-best-practices/  
[5] https://www.hivemq.com/blog/mqtt-essentials-part-7-persistent-session-queuing-messages/  
[6] https://www.rabbitmq.com/mqtt.html  
