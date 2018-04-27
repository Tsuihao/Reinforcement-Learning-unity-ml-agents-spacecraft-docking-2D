# **PPO training documentation** 


---

**Spacecraft docking**

The goals / steps of this project are the following:
* Design an effiencet and correct reward function

[spacecraft_0]: ./Tensorboard/Spacecraft_0/spacecraft_0.png 
[spacecraft_1]: ./Tensorboard/Spacecraft_1/spacecraft_1.JPG 


## Template
**Date:**<br>
**Reward function:**<br>
**Observation:**<br>
**Improved points:**<br> 
**Need improvements:**<br>
**Training image:** <br>


---

### Spacecraft_0


**Date:** 4/26 <br>
**Reward function:** position <br>
**Observation:** The spacecraft uses rotation to slow approach to the target. <br>
**Improved points:**<br> 
**Need improvements:** 
* without rotation reward
* spacecraft does not learn from collision	
* success and failure count has bug	(solved)
* position reward is too high	
* step punishment should be added	
* collision punishment should be added
* another reward function: maybe add a reward that after certain distance to the target point, the algorithm starts to adjust the reward function <br>

**Training image:** <br>
![alt text][spacecraft_0]

---
### Spacecraft_1

**Date:** 4/27 <br>
**Reward function:** position, step, orientation, position orientation <br>
**Observation:**
* Bad behaviour 
* Why position reward is the most weigthed but spacecraft do not know to apporach it?
* Negative reward is too much than positive 
* The second stage position reward function need to be modified: There is always the case that the spacecraft is slightly rotation near the space station but due the heavily negative reward at second stage, the total reward just become native. 
-> **Solution**: cancel the negative reward at second stage, for orientation reward and position reward?
* The initialization range should be increased.

<br>
**Improved points:**
* solved previous bug by adjusting agents' decision frequency to 1 (in the inspector)
* Add step reward function
* Add orientation reward funciton
* Add position orientation reward function (distinqush with orientation reward function, see in comment of the spacecraftAgent.cs)
* Classify docking range into three stages: initial, first, and second by the distance to target dokcing point. 
* Add tracing for all reward functions values

* (Minus point): Changed orientation to non-rigid body rotation <br> 

**Need improvements:**
* orientation reward function scale is too sensitive
* position orientation reward function scale is too non-sensitive <br>

**Training image:** <br>
![alt text][spacecraft_1]

---