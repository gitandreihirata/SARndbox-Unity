
#  SARndbox V2

# Release Notes — AR Sandbox Water Simulation v1.3

## 🚀 Overview

Esta versão traz uma reformulação completa da arquitetura de reconhecimento de gestos (MediaPipe) e do sistema de simulação de água. O processamento das interações foi centralizado, diversos problemas de lógica e renderização foram corrigidos e novas mecânicas espaciais foram adicionadas para tornar a experiência mais intuitiva e realista.

---

# ✨ New Features

### 🌬️ Wind Physics (Open Palm Gesture)

Implementado um sistema de vento baseado em cinética (`ΔP/Δt`). Ao realizar um movimento rápido de varredura com a mão aberta, a velocidade do gesto é convertida em vetores de força horizontal (`ApplyWindForce`), permitindo empurrar as partículas de água e criar simulações dinâmicas de dispersão.

### 🪨 Interactive 3D Objects

#### Spawn (ILoveYou Gesture)

O gesto **ILoveYou** agora instancia um objeto interativo (como um bloco de contenção) no centro da sandbox.

#### Grab & Drop (Lasso Grab)

O gesto **Lasso_Grab** permite selecionar, mover e soltar objetos.

Durante a movimentação, a altura da areia é calculada em tempo real utilizando `Sandbox.GetDepthFromWorldPos()`, permitindo que o objeto acompanhe naturalmente montanhas e vales da superfície.

### ♻️ Global Reset (Victory Gesture)

O gesto **Victory (✌️)** agora executa uma limpeza completa da simulação, removendo simultaneamente:

* partículas de água;
* emissores de cachoeira;
* nuvens;
* objetos interativos.

### 💧 Independent Waterfall Emitters

O controle de emissão (`EmissionRateSlider`) foi reescrito.

Agora, alterações no slider afetam apenas novas cachoeiras criadas, preservando a taxa de emissão das cachoeiras já existentes.

---

# 🐛 Bug Fixes

### ✔️ Waterfall Overlap Prevention

Corrigida a lógica de verificação de distância (`checkDistance`), que agora analisa todos os emissores ativos antes de criar uma nova cachoeira, evitando sobreposição.

### ✔️ Camera Clipping Issues

Ajustadas as alturas de spawn dos projetores:

* Cloud: `-150f → -60f`
* Water: `-50f → -40f`

Eliminando problemas de clipping e objetos invisíveis.

### ✔️ Cloud Lifecycle Restoration

Removido código legado que destruía os componentes:

* `SimpleCloudBehavior`
* `CloudLifeCycle`

As nuvens voltaram a executar corretamente suas animações e ciclo de vida.

### ✔️ Complete Water Cleanup

Refatorada a rotina `DestroyWaterDroplets()` utilizando `FindObjectsOfType()`.

Agora todas as partículas de água são removidas em um único frame, incluindo aquelas geradas automaticamente pelas cachoeiras.

### ✔️ Gesture Detection in Shallow Sandboxes

Removida a verificação `Physics.CheckSphere` para os gestos da mão.

Isso elimina falsos bloqueios em caixas de areia rasas e garante funcionamento consistente da interação.

### ✔️ Closed Fist Gesture Independence

O gesto **Closed_Fist** não depende mais da flag `isWaterfallActive`.

Agora:

* o gesto sempre cria uma cachoeira;
* o toggle da interface controla apenas a interação via mouse.

### ✔️ Pointing Gesture False Positives

Adicionado um filtro para ignorar o gesto **Pointing** dentro do `WaterSimulation`.

Isso impede a criação acidental de água durante o uso da ferramenta de leitura topográfica a laser.

---

# 🛠️ Architecture & Refactoring

## HandInput.cs Simplification

A classe `HandInput` deixou de ser responsável pela lógica de jogo.

Agora ela atua exclusivamente como uma camada de entrada responsável por enviar:

* posição da mão;
* identificação dos gestos.

## Centralized Gesture Processing

Toda a lógica da simulação foi migrada para `WaterSimulation.cs`, concentrando o processamento no método:

```csharp
OnGesturesReady()
```

Agora este método é responsável por:

* criação de água;
* cálculo do vento;
* movimentação de objetos;
* leitura da profundidade da sandbox;
* gerenciamento dos gestos.

## Input Isolation

Foi implementada a flag:

```csharp
gesture.IsUIGesture
```

Essa separação isola completamente:

* entradas da Interface (Mouse/Touch)
* entradas do MediaPipe (IA)

permitindo que ambos os sistemas operem simultaneamente sem conflitos.

---

# 📈 Summary

### Added

* Wind simulation using Open Palm gesture.
* Interactive object spawning.
* Object Grab & Drop.
* Global Reset gesture.
* Independent waterfall emission rates.

### Fixed

* Waterfall overlap.
* Camera clipping.
* Cloud lifecycle.
* Complete water cleanup.
* Gesture detection in shallow sandboxes.
* Closed Fist dependency.
* Pointing gesture false positives.

### Refactored

* Simplified `HandInput`.
* Centralized gesture processing in `WaterSimulation`.
* Input isolation between UI and MediaPipe.


V1.2
 
* Atualizado para a versão 2017.41f para 2022.3.60f1
* Tela que valida quando o Kinect não está sendo identificado;
* Traduções em Portuguese (pt).asset em traduções fixas e por código. suporte ingles, português, espanhol e multi idiomas pode ser feito;
* Melhoria nos componentes para não cortar as traduções;
* Mudar a cor da água geral de acordo com o padrão: ácido, lava, óleo;
* Mudar a viscosidade da água separadamente;
* Configurar a velocidade de absorção do solo e de evaporação da água;
* Inclusão e Controle da velocidade do fluxo da cachoeira e chuva (quantidade de partículas a ser emitida). chuva a mao tem q ficar, cachoeira é inserido uma area aonde fica infinitamente caindo agua
* Adicionado gestos de mao aberta e fechada usando identificação geométrica para chover e não chover  a partir de uma altura na configuração;
* Integração com mediapipe para usar todos os gestos disponveis do framework e programação inicial do gesto Chuva;


# SensiLab AR Sandbox
![SensiLab AR Sandbox](https://sensilab.monash.edu/new-sensilab/wp-content/uploads/2018/06/43I5615.jpg)
This project is a from-scratch Unity rebuild and extension of the AR Sandbox project developed by the [KeckCAVES group at UC Davis](https://web.cs.ucdavis.edu/~okreylos/ResDev/SARndbox/)

The new version was developed in [SensiLab](https://sensilab.monash.edu) at Monash University, Melbourne, Australia

## BAIXE O TUTORIAL
Baixe o tutorial para a utilização do projeto [aqui](https://drive.google.com/file/d/1S4aQGnXGdHpr4u0eP235gyj-Lj73dYv9/view?usp=sharing).

 ## Requisitos para executar o projeto
 * Windows 7 ou mais novo
 * Kinect 2

## Licença
GNU General Public License v3.0 or later

See [COPYING](COPYING) to see the full text
