
#  SARndbox V2

 ## Atualizações

V1.3

🚀 Release Notes: AR Sandbox - Water Simulation v1.3Esta atualização traz uma reformulação completa na arquitetura de reconhecimento de gestos (MediaPipe) e no motor de física de fluidos. O processamento de interações foi centralizado, bugs visuais e de lógica foram corrigidos, e novas mecânicas espaciais foram introduzidas para melhorar a experiência tangível.

✨ Novas Funcionalidades (Features)Física de Vento (Gesto Mão Aberta / Open_Palm):Implementada mecânica de cinética baseada em $\Delta P / \Delta t$. Ao realizar um movimento rápido de varredura (swipe) sobre a caixa, o sistema converte a velocidade da mão em vetores de força horizontal (ApplyWindForce), empurrando as partículas de água e permitindo estudos dinâmicos de dispersão.Manipulação de Objetos 3D (Gestos ILoveYou e Lasso_Grab):Spawn (Homem-Aranha): O gesto ILoveYou agora instancia um objeto interativo (ex: bloco de contenção) no centro geográfico da caixa de areia.Grab & Drop (Pinça): O gesto Lasso_Grab permite agarrar e arrastar o objeto. A lógica calcula a altura real da areia (Sandbox.GetDepthFromWorldPos) em tempo real, permitindo que o objeto suba montanhas e desça vales fisicamente.Limpeza Global via Gesto (Victory):O sinal de "V" (Paz e Amor) agora atua como um Global Reset, destruindo simultaneamente gotas de água, emissores de cachoeira/nuvem e objetos interativos.Emissores de Cachoeira Independentes:O controle de velocidade da UI (EmissionRateSlider) foi reescrito. Agora, alterar o slider afeta apenas as novas cachoeiras criadas, mantendo as antigas operando na velocidade em que foram instanciadas.

🐛 Correções de Bugs (Bug Fixes)Fix: Nuvens sobrepostas (Overlap Spawn): A verificação de distância (checkDistance) agora varre a lista completa de emissores ativos, impedindo que novas cachoeiras nasçam dentro do raio de colisão de qualquer cachoeira existente.Fix: Profundidade do Clipping de Câmera (Z-Axis): Ajustada a altura de spawn dos projetores de nuvem (de -150f para -60f) e água (de -50f para -40f), evitando que fiquem invisíveis ou cortados pelo Clipping Plane do projetor.Fix: Lobotomia de Nuvens (Script Destroy): Removidas linhas de código legadas que deletavam os componentes SimpleCloudBehavior e CloudLifeCycle no momento do spawn, devolvendo as animações e o ciclo de vida às nuvens.Fix: Limpeza Incompleta de Água: O botão "Limpar" e a função DestroyWaterDroplets foram refatorados para usar FindObjectsOfType<WaterDroplet>(). Agora ele destrói todas as partículas no cenário em 1 frame, incluindo as geradas de forma autônoma pelas cachoeiras (que antes ficavam presas na areia).Fix: Mão invisível em caixas rasas: Removido o bloqueio Physics.CheckSphere dos gestos da mão. A calibragem de caixas mais rasas não aciona mais colisões falsas, garantindo que a água flua da mão independente da altura da areia.Fix: Dependência de UI no Gesto Fechado (Closed_Fist): Removida a exigência da flag isWaterfallActive para o gesto de mão fechada. A cachoeira agora nasce naturalmente pelo gesto, enquanto o toggle da UI controla exclusivamente a interação do Mouse.Fix: Falso Positivo no Gesto Pointer (Pointing): Adicionado um filtro de escape para o gesto de Apontar. O WaterSimulation agora ignora este gesto, não instanciando mais água acidentalmente enquanto o usuário usa a ferramenta de leitura topográfica a laser.

🛠️ Refatoração e ArquiteturaDescentralização do HandInput.cs:A classe HandInput foi enxugada e não atua mais como criadora de eventos de jogo. Ela agora funciona exclusivamente como Mensageira, enviando coordenadas e identificações de gestos.O "Maestro" WaterSimulation.cs:Toda a lógica de instanciar água, ler distâncias, calcular ventos e mover objetos foi migrada para o método OnGesturesReady() dentro de WaterSimulation.cs.Isolamento de rotinas: Criada uma proteção (gesture.IsUIGesture) que separa completamente o tráfego de dados do Mouse/Touch dos dados vindos da Inteligência Artificial (MediaPipe), garantindo que as duas interfaces funcionem simultaneamente sem conflito.

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
