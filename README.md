# 月球建造模拟 - Rhino/CAD风格VR建模工具

![Unity](https://img.shields.io/badge/Unity-2022.3+-000?style=flat&logo=unity)
![Meta Quest](https://img.shields.io/badge/Meta%20Quest-3-000?style=flat&logo=oculus)
![VR](https://img.shields.io/badge/Interaction-SDK-000?style=flat&logo=unity)

**一个基于 Unity 与 Meta Quest 3 的沉浸式 Rhino/CAD 风格建模工具，旨在模拟远程操控月球 3D 打印 rover 进行建造。**

---

![项目VR界面演示](./media/微信图片_20251124203543.png)


## 🎯 项目概述

本项目探索了如何在虚拟现实（VR）环境中移植并增强传统的 CAD 建模工作流。用户可在一个沉浸式的月球场景中，通过直觉化的手势交互，完成 **创建→预览→编辑→管理** 物体的完整循环。项目不仅实现了核心建模功能，更通过系统性的用户测试对交互设计进行了验证与迭代。

## ✨ 核心功能

- **沉浸式建模循环**：支持创建立方体、圆柱体等几何体，提供 **实时放置预览**、**网格吸附** 与 **堆叠** 功能。
- **完整对象编辑**：实现物体的 **选择、移动、旋转、缩放与删除**。
- **材质系统**：新增 `Change Material` 功能，切换不同视觉质感（光泽、金属、粗糙），确保用户感知为 **材质变化** 而非单纯换色。
- **直觉化VR交互**：采用 **Poke交互** 与 **Quick Actions UI**，配合 **官方Teleport** 方案进行空间移动。

## 🛠 技术实现

- **开发平台**：Unity Engine, Meta Interaction SDK & Building Blocks
- **核心交互**：手部追踪，Poke交互式UI，物体变换控制（Transform）
- **渲染与视觉**：URP/HDRP渲染管线，动态材质切换（Material Property Block）
- **移动方案**：定制化Meta官方Teleport组件

## 📊 用户测试与迭代

项目包含完整的可用性测试流程（N=10，Task-based + Think-aloud），并基于数据驱动进行了设计迭代：

- **发现痛点**：识别出旋转精度不足、垂直移动不便、UI提示不明显三大问题。
- **迭代方案**：设计了**轴向锁定旋转**、**垂直传送锚点**、**UI动效与提示优化**等改进方案。

## 🚀 项目亮点

- **全流程实践**：覆盖从概念设计、功能开发、用户测试到数据驱动迭代的完整项目周期。
- **系统化思维**：精准定位体验瓶颈，并提出具体可行的技术解决方案，展示了超越功能实现的系统设计能力。

---

## 📁 项目结构


## Project Structure Overview

### Documentation

- **[Documentation](./Documentation/README.md)** - Complete documentation structure and navigation


### Prototypes

- **[Prototype 1](./Prototype%201/README.md)** - Horizontal Unity Prototype
- **[Prototype 2](./Prototype%202/README.md)** - XR Prototype 1
- **[Prototype 3](./Prototype%203/README.md)** - XR Prototype 2

### Test Projects

- **[Test Projects](./TestProjects/README.md)** - Weekly Unity activities and experimental projects.
