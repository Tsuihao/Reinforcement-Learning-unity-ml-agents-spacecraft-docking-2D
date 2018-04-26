# **PPO training documentation** 


---

**Spacecraft docking**

The goals / steps of this project are the following:
* Design an effiencet and correct reward function

[//]: # (Image References)

[spacecraft_0]: ./Tensorboard/Spacecraft_0/spacecraft_0.png 

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

**Date:** 4/26 <br>
**Reward function:** position, step <br>
**Observation:**<br>
**Improved points:**
* solved previous bug by adjusting agents' decision frequency to 1 (in the inspector)
* Add step reward function <br> 

**Need improvements:**<br>
**Training image:** <br>