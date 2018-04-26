# **unity3D ml-agents spacecraft docking** 
[unity3d ml-agents](https://unity3d.com/machine-learning)


Overview
---
Implementing a spacecraft docking scenario via ml-agents library provided by unity3D.
The spacecraft (agent) is trained with [Proximal Policy Optimization (PPO)](https://blog.openai.com/openai-baselines-ppo/), a reinforcement learning algorithm.
The goal of this project is to let spacecraft (agent) dock to the space station successfully under the physical constrains (rigid body).

Challenge
---
The challenge of this project are following:
1. Design an efficient reward function 
2. Consider constrains for docking scenarios e.g.(position, orientation, velocity, angular velocity, etc.)
3. Hyper parameters tuning e.g.(movement speed, orientation speed, etc.) 


