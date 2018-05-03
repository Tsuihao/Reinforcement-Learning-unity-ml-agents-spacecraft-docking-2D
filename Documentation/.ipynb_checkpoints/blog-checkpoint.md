# **PPO training documentation** 


---

**Spacecraft docking**

The goals / steps of this project are the following:
* Design an effiencet and correct reward function

[spacecraft_0]: ./Tensorboard/Spacecraft_0/spacecraft_0.png 
[spacecraft_1]: ./Tensorboard/Spacecraft_1/spacecraft_1.JPG 
[spacecraft_2]: ./Tensorboard/Spacecraft_2/spacecraft_2.JPG 

## Template
**Date:**<br>
**Reward function:**<br>
**Observation:**<br>
**Improved points:**<br> 
**Need improvements:**<br>
**Training image:** <br>


---
## Spacecraft_New
A huge modification is done during the refactoring of the whole structure.
**The previous designs are all changed** (referring to Spacecraft_0, Spacecraft_1 and Spacecraft_2 at below). While viewing the other ml-agent provided examples, we realized that the **_conventional_** reward functions we provided before is not fully utilize the power of reinforment learning. In addition, the **rayPerception** class offers the visual ability for agents which directly helps us to eliminate the previous position reward function (the closer to the docking point, the higher). Here we will list some points are improtant for us.

**[Lesson learned]**
* Time pressure reward function helps agents to finish the task faster AddReward(-1/agent.maxstep)

* Ray perception provides the visibility of agent (combining with angle of view and range of view)

* The training result can be transferable e.g if the agent is trained with the fixed docking point, it will still know to find the floating docking points (but this need to be combined with **rayPerception**)


**Date:** 5/3<br>
**Reward function:** 

Time pressure reward, guidance reward, attitude control reward, perfect docking (additional reward)<br>

**Observation:** 
* When initialization position of spacecraft is at the upper part of the docking point still has problem 

* Perfect docking is not efficient (due to attitude control reward fucntion is not efficient)<br>

**Improved points:**
* Success rate increase
* Visualization of the gaming improved <br>
 
**Need improvements:**
* The performance of attitude reward fucntion is still not good. The velocity during dockingn is still too fast.<br>

**Training image:** <br>


---
## [Legacy]
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
-> **Proposal solution**: cancel the negative reward at second stage, for orientation reward and position reward?
* The initialization range should be increased.
* Not sure does the agent learn to avoid the obstacles?
* Step negative is too much <br>

**Improved points:** <br>

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
## Spacecraft_2

**Date:** 4/27 <br>
**Reward function:** same as previous <br>
**Observation:** 
* Orientation is wrong 
* Somehow the spacecraft is not knowing to approaching the target(may be the position orientation reward is conflict, due to it is only care about "below the station") **Proposal solution**: cancel the position orientation reward at the initial stage<br>

**Improved points:**<br> 
**Need improvements:** reward functions<br>
**Training image:** 
![alt text][spacecraft_2]
<br>

 --- 
