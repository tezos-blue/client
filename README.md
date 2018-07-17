# tezos.blue | Client.Engine
cross-platform client library for remote access to the Tezos network

The wallet built upon this engine can be downloaded via

http://tezos.blue

for Android, iOS and Windows

## About
This libraries purpose is to provide comfort and security for .NET Developers building client applications for the Tezos network.

It is written in C# and based on the .NET Standard 2.0

## Comfort
Comfort means to translate the language of the blockchain and its internals into semantics that are matching the language of real-world problem domains, driven by use cases.

It also means having full access to the capabilities of Tezos, while not having to run a node locally. Minimum resource usage on the client side for mobile development.

Finally, this library enables developers to write cross-platform code for all operation systems supported by the .NET Standard 2.0

## Security
To build trust with users of applications built upon the Client.Engine, a two stage proof is proposed:

1. It is proven by code reviews that no secret entered into the Engine will ever leave it without being heavily encrypted by the owner personally.
While in the Engine, all possible measures are taken to protect the secret from theft.

2. All applications are bound by the license to make their code open-source, so that users can review the path of their secrets down to the Engine and make sure, the developer has not accidentally forgotten not to store them on the way.

If you volunteer to review the security, please take a look at

[Security Reviewer's Guide](https://github.com/tezos-blue/client/blob/master/SecurityReview.md)

## tezos.blue
The tezos.blue wallet is an example of an application built on this Engine. 

The tezos.blue backend system is an online, highly scalable and performant implementation of the `Connection` interface required for the interaction of the Engine with the Tezos network. 

## Notes to Developers
There will be a NuGet package for an easy connection to the live tezos.blue system. Please be patient.

Currently, the only way to use this Engine is to initialize it with a network simulation, which is included.
This is also how the tests in the Engine.Test project are executed. 
You will find a good entry point into the general idea there.

Version 0.3 will focus on security entirely. Your critical view and input during the coming weeks is highly welcome.

https://tezos.blue


