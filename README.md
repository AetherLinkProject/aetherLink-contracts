# aetherLink-contracts

1. `Consumer contract` For contracts deployed by users, the official interface proto file for initiating tasks and receiving results is provided, and users need to reference and implement it. 
2. `Coordinator contract` Official contract. According to the currently provided products PriceFeeds and VRF, two corresponding Coordinator contracts need to be deployed respectively.
3. `Oracle Contract` The officially provided contract decouples the Oracle node and Consumer contracts from the business. 

## Installation

Before cloning the code and deploying the contract, command dependencies and development tools are needed. You can follow:

- [Common dependencies](https://aelf-boilerplate-docs.readthedocs.io/en/latest/overview/dependencies.html)
- [Building sources and development tools](https://aelf-boilerplate-docs.readthedocs.io/en/latest/overview/tools.html)

The following command will clone Aetherlink Contract into a folder. Please open a terminal and enter the following command:

```Bash
git clone https://github.com/AetherLinkProject/aetherLink-contracts.git
```

The next step is to build the contract to ensure everything is working correctly. Once everything is built, you can run as follows:

```Bash
# enter the Launcher folder and build 
cd src/AElf.Boilerplate.Aetherlink.Launcher

# build
dotnet build

# run the node 
dotnet run
```

It will run a local temporary aelf node and automatically deploy the Aetherlink Contract on it. You can access the node from `localhost:1235`.

### Test

You can easily run unit tests on Aetherlink Contracts. Navigate to the Aetherlink.Contracts.Tests and run:

```Bash
cd ../../test/Aetherlink.Contracts.Tests
dotnet test
```

## Contracts

The oracle contract consists of three types of contracts:
- `Consumer contract` There are two main functions:
    1. Initiate the task function. Initiated by the user, the Oracle contract will be called to complete subsequent operations.
    2. Receive result function. The Oracle contract calls back and writes the results back to the user contract, and the user implements the data storage logic by himself.
- `Coordinator contract` There are three main functions:
    1. Task management function. Generate a unique ID for the task, as well as task details, and store them in the contract.
    2. Threshold signature verification (non-algorithm verification threshold) / VRF Proof verification function. After the node submits the task result, if it is a PriceFeeds-type task, the Coordinator contract is responsible for verifying the threshold signature of the submitted signature; if it is a VRF-type task, the Coordinator contract needs to restore the random hash through Proof.
    3. Bill management function (not implemented in this issue, will be implemented iteratively in the next version). Implement the function of collecting user fees and rewarding Oracle nodes based on task completion and node participation.
- `Oracle Contract` There are three main functions:
    1. Subscription function. Provide subscription management functions for user contracts and initiate task requests through subscriptions
    2. Node management function. Implemented registration and role assignment of Oracle nodes, and configurable parameters for threshold signing.
    3. Event-driven functionality. The Oracle node needs to listen to the events of this contract and trigger the node to perform corresponding operations by throwing events.

## Contributing

We welcome contributions to the Aetherlink Contract project. If you would like to contribute, please fork the repository and submit a pull request with your changes. Before submitting a pull request, please ensure that your code is well-tested and adheres to the aelf coding standards.

## License

AetherLink Contract is licensed under [MIT](https://github.com/AetherLinkProject/aetherLink-contracts/blob/master/LICENSE).