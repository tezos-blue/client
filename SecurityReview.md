# Guide for Security Reviewers
The basic security architecture is in place. But, to be honest, the expertise over here, when it comes to hardcore security, might very well prove to be inadequate.

I invite and welcome your expert view on this security concept and its implementation. Please help to harden the security of identities entrusted to tezos.blue.

Feel free to raise issues, voice concerns, make suggestions or send pull requests.
Here or via hello@tezos.blue




## Overview
The paramount task of security in tezos.blue is:

*Protect the private key from theft*

That translates into this general policy:

1. Do not need the key in the first place
2. If they entrust it to you, protect it strongly
3. Get rid of it quickly

To enforce these, all secrets travel through well-defined channels.

## Signing providers
The only place and time a private key is needed is: the signing of operations.

Since this boils down to applying a cryptographic signature to some data, it can easily be abstracted away in a simple interface. 

Any piece of code that implements

```csharp
public interface IProvideSigning
{
	IEnumerable<string> IdentityIDs { get; }

	Task Initialize();

	Task<bool> Sign(string identityID, byte[] data, out byte[] signature);

	bool Contains(string identityID);

	byte[] GetPublicKey(string identityID);
}

```

can be registered with the Engine and enjoys full integration.

This should make it natural to plug in hardware wallets, thus removing the need to expose the private key at all.


For the alleged majority of users without hardware wallets, the client engine provides an implementation of ```IProvideSigning```, trying to get as close as possible to the security of hardware.


## SoftwareVault
With no other identity provider configured, the Engine defaults to an internal implementation for signing.

```csharp
public class SoftwareVault : IManageIdentities, IProvideSigning
```
Apart from signing, the vault provides services as a manageable container of identities. It can
- Create and manage identities
- Protect their secrets
- Assist in secure storage

The vault follows 2 iron rules

1. Any secret entering will never leave, unless encrypted
2. While inside, the secret is protected by all means

Protection of secrets inside the memory space of the vault is currently done by software encryption.

## Secrets
All secret data, keys or passphrases, are stored in classes deriving from
```csharp
public abstract class Secret : IDisposable
```
This class encrypts the secret's data in memory and randomizes the traces on disposal.

For application code, the data is transparently accessible.
External intruders into the process memory space shall have a hard time, though.

Built upon `Secret` is the next level of protection:

```csharp
public abstract class PhraseProtectedSecret : Secret
```

This class encrypts/decrypts secret data with a passphrase on top of the memory scrambling.

No code, not even the Engine, can read the secret data without providing the passphrase.

Passphrases themselves are protected, too.

```csharp
public sealed class Passphrase : Secret
```

Once initialized, no client code, not even the Engine, can access the clear text inside a Passphrase.


## Notes to Reviewers

All places relevant to security in the code are marked with
```csharp
// SECURITY
```
To get a general feeling for usage and semantics of the client engine, I recommend reading test cases in the [Test](https://github.com/tezos-blue/client/tree/master/Client/Test) project.

#### Signing
You will go to the heart of the matter, if you look at

[The Generic Sign Test](https://github.com/tezos-blue/client/blob/master/Client/Test/SecurityTest.Sign.cs)

and, correspondingly, the code in the Engine

[The Signing Topic](https://github.com/tezos-blue/client/blob/master/Client/Engine/Topics/Signing.cs)

#### SoftwareVault
All relevant code in the [Engine/Security](https://github.com/tezos-blue/client/tree/master/Client/Engine/Security) folder.

[SoftwareVault.cs](https://github.com/tezos-blue/client/blob/master/Client/Engine/Security/SoftwareVault.cs)

#### Secrets
Realized in the [Cryptography](https://github.com/tezos-blue/client/tree/master/Cryptography) project where also all cryptographic algorithms are integrated. Look at

[Secret.cs](https://github.com/tezos-blue/client/blob/master/Cryptography/Secret.cs)

[Passphrase.cs](https://github.com/tezos-blue/client/blob/master/Cryptography/Passphrase.cs)

[CryptoServices.cs](https://github.com/tezos-blue/client/blob/master/Cryptography/CryptoServices.cs)

Thanks for your time and knowledge.


