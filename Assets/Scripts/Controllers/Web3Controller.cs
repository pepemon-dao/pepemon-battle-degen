using Nethereum.Hex.HexTypes;
using Nethereum.Web3.Accounts;
using UnityEngine;
using UnityEngine.Events;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using System.Linq;
using System;
using Thirdweb;
using System.IO;

public class Web3Controller : MonoBehaviour
{
    public static Web3Controller instance;

    public Web3Settings settings;
    public UnityEvent onWalletConnected;
    public int CurrentChainId { get; private set; } = 0;

    #region ThirdWeb
    [Header("Events")]
    public UnityEvent<WalletConnection> onConnectionRequested;
    public UnityEvent<string> onConnected;
    public UnityEvent<Exception> onConnectionError;
    #endregion

#if UNITY_EDITOR
    #region Unity editor button for updating hardhat contracts addresses
    [Serializable]
    struct DeploymentJson
    {
        public string address;
    }

    [Button(), PropertyTooltip("Update hardhat's addresses (chainId 31337) from localhost deployment," +
        "this way there is no need to copy the contracts' addresses from the terminal")]
    private void UpdateHardhatAddresses()
    {
        // use hardhat local deployment assuming that the battle-contracts project is on ../
        var deployments = @"..\battle-contracts\deployments\localhost";
        
        // get hardhat config
        var chain = settings.chains.FirstOrDefault(x => x.chainId == 31337);
        var index = Array.IndexOf(settings.chains, chain);

        chain.pepemonBattleAddress = JsonUtility.FromJson<DeploymentJson>(
            System.IO.File.ReadAllText($@"{deployments}\PepemonBattle.json")).address;
        Debug.Log("Set pepemonBattleAddress = " + chain.pepemonBattleAddress);

        chain.pepemonCardDeckAddress = JsonUtility.FromJson<DeploymentJson>(
            System.IO.File.ReadAllText($@"{deployments}\PepemonCardDeck.json")).address;
        Debug.Log("Set pepemonCardDeckAddress = " + chain.pepemonCardDeckAddress);

        chain.pepemonMatchmakerAddresses[0] = JsonUtility.FromJson<DeploymentJson>(
            System.IO.File.ReadAllText($@"{deployments}\PepemonMatchmaker.json")).address;
        Debug.Log("Set pepemonMatchmakerAddresses[0] = " + chain.pepemonMatchmakerAddresses[0]);

        settings.chains[index] = chain;
    }
    #endregion
#endif

    [HideInInspector] public bool IsConnected = false;
    [HideInInspector] public ulong StarterDeckID = 0;
    [HideInInspector] public int StarterPepemonID = 0;

    private void Awake()
    {
        if (instance == null)
        {
            DontDestroyOnLoad(this);
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        CurrentChainId = settings.defaultChainId;
    }

    public async UniTask ConnectWallet()
    {
        Debug.Log("Trying to connect");

        var provider = WalletProvider.LocalWallet;
#if UNITY_EDITOR
        await WriteDebugLocalAccount();
#elif PLATFORM_WEBGL
        provider = WalletProvider.Metamask;
#else
        provider = WalletProvider.WalletConnect;
#endif
        var wc = new WalletConnection(provider, settings.defaultChainId);
        onConnectionRequested.Invoke(wc);
        try
        {
            var address = await ThirdwebManager.Instance.SDK.Wallet.Connect(wc);
            onConnected.Invoke(address);
            IsConnected = true;
            onWalletConnected?.Invoke();
        } 
        catch (Exception e)
        {
            Debug.LogError($"unable to connect wallet: {e}");
            onConnectionError.Invoke(e);
        }
    }

    private async UniTask WriteDebugLocalAccount()
    {
#if UNITY_EDITOR
        // Thirdweb provides no easy way of connecting a wallet using hardhat's private key,
        // The lines below create a file with the private key. If it didn't exist, Thirdweb would generate
        // a random wallet instead.
        var key = new Nethereum.Signer.EthECKey(settings.debugPrivateKey);
        var path = Utils.GetAccountPath();
        var deviceId = Utils.GetDeviceIdentifier();
        Debug.Log("Encrypting and writing debug key...");
        await UniTask.RunOnThreadPool(() => 
        {
            var content = Utils.EncryptAndGenerateKeyStore(key, deviceId);
            File.WriteAllText(path, content);
        });
        Debug.Log("Done");
#endif
    }

    public Web3Settings.Web3ChainConfig GetChainConfig()
    {
        return settings.GetChainConfig(CurrentChainId);
    }
}
