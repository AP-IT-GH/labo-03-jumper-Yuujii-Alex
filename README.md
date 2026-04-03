# Verslag: Reinforcement Learning met ML-Agents - Hunter vs Prey rapport

## Inleiding

Dit rapport documenteert het ontwerp en de training van twee simultaan lerende reinforcement learning agents in Unity via de ML-Agents toolkit. Het doel van dit experiment is te onderzoeken hoe twee agents met tegengestelde doelstellingen, een hunter en een prey, elkaars leerproces beïnvloeden. De hunter heeft als taak de prey te vangen, terwijl de prey fruit moet verzamelen en tegelijkertijd aan de hunter moet ontsnappen. Dit rapport is bestemd voor studenten en begeleiders die vertrouwd zijn met de basisconcepten van reinforcement learning.

De centrale onderzoeksvraag luidt: hoe kunnen belonings- en strafstructuren zo worden opgebouwd dat beide agents een functioneel en stabiel gedragspatroon ontwikkelen zonder ongewenst lokaal gedrag?

## Methoden

De simulatieomgeving bestaat uit een platform van schaal X=Y=Z=2, omsloten door vier muren, aangemaakt in Unity. Bij de start van elke episode worden de twee agents en de rode fruitobjecten op willekeurige posities geplaatst. Beide agents zijn uitgerust met een **Ray Perception Sensor 3D** met 6 rays per richting en een observatiehoek van 180 graden.

**Agents:**

- **Hunter** (Rood): probeert de prey te vangen; is iets trager dan de prey om de balans te bewaren. De hunter ontvangt geen informatie over de locatie van fruit via de raycasts, om te vermijden dat hij fruit beschermt in plaats van de prey achtervolgt.
- **Prey** (Blauw): probeert fruit te verzamelen en de hunter te ontwijken; is iets sneller dan de hunter.

**Gedeelde regels:**

- Beide agents mogen de muren niet raken (geeft een straf).
- De episode eindigt wanneer de hunter de prey vangt, prey al het fruit verzameld, of na een maximaal aantal stappen.

Het reward-systeem werd iteratief aangepast over drie trainingsrondes.

## Resultaten

### Training 1

**Observaties:** In de eerste training ontving de prey een continue positieve reward op basis van de afstand tot de hunter, hoe verder weg, hoe meer reward. De hunter ontving een reward telkens hij de prey naderde.

Het lijkt erop dat de prey in Training 1 een lokale strategie heeft aangeleerd waarbij wegblijven van de hunter consistenter werd beloond dan het oppakken van fruit. Fruit oppakken leverde minder op dan stabiel op afstand blijven. Tevens werd bij het meegeven van fruitposities aan de hunter via de perception sensor waargenomen dat de hunter het fruit beschermde in plaats van de prey achtervolgde.

<img width="330" height="205" alt="image" src="https://github.com/user-attachments/assets/216c7924-5b16-4ffc-824d-e5890addf31b" />
<img width="181" height="55" alt="image" src="https://github.com/user-attachments/assets/df2a7099-6810-43b3-894b-797024f468db" />

De leercurve in Training 1 vertoont onregelmatig gedrag. De hunter convergeert relatief snel, terwijl de prey de neiging vertoont in hoeken te gaan staan in plaats van actief te bewegen. Dit wijst erop dat de reward-structuur voor de prey niet voldoende richting geeft naar het gewenste doelgedrag.

---

### Training 2

**Aanpassingen:** De afstandsgebaseerde reward voor de prey werd verwijderd. De prey ontvangt enkel nog rewards door fruit op te pakken. De positie van de prey wordt aan de hunter doorgegeven via een relatieve transformatie: `sensor.AddObservation(transform.InverseTransformPoint(prey.transform.position))`. Hierdoor beschikt de hunter over richtingsinformatie ten opzichte van zichzelf, in plaats van absolute kaartcoördinaten.

**Observaties:** Er is een merkbaar snellere convergentie van de hunter waarneembaar in vergelijking met Training 1. De hunter ontwikkelt een achterwaartse bewegingsstrategie (het bewegen met de achterkant naar voren), wat vermoedelijk de wendbaarheid vergroot. Om dit ongewenste gedrag te ontmoedigen, wordt een straf van -0.02 per stap achterwaarts ingevoerd, gecombineerd met een reward van +0.02 wanneer de hunter de prey nadert.

Het lijkt erop dat positieve rewards op basis van afstandsvermindering tot fruit leiden tot ronddraaiend gedrag bij de prey, omdat kleine afstandswinsten via rotatiebeweging sneller geaccumuleerd worden dan via rechtstreeks bewegen. Om deze reden werd overgeschakeld naar een straf wanneer de prey te ver van het dichtstbijzijnde fruit verwijderd is. In de meeste gevallen is het beter om af te straffen in plaats van positieve rewards te geven. Bij positieve rewards moet je ook nadenken over hoe agents dit graag misbruiken om meer rewards te krijgen. Door hier positieve rewards te geven krijg je onverwacht gedrag, bijvoorbeeld in cirkels te draaien krijgt de prey agent consistent rewards. Door een afstraffing te geven gebruik je dezelfde logica maar andersom, en moet je geen rekening houden met het in rondjes draaien en de agent dit te misbruiken.

<img width="321" height="206" alt="image" src="https://github.com/user-attachments/assets/3146ec2c-db58-46b3-a9f3-848d3c0e7d17" />
<img width="185" height="54" alt="image" src="https://github.com/user-attachments/assets/4c2fad3d-cc89-4279-a85d-7d366c1b9f91" />

De leercurve in Training 2 is merkbaar stabieler dan in Training 1. Beide agents vertonen een vroegere convergentie. De prey blijkt echter alsnog moeite te hebben met het gericht oppakken van fruit, wat suggereert dat de reward-structuur voor de prey verder verfijnd dient te worden.

---

### Training 3

**Aanpassingen:** De reward voor het oppakken van fruit werd licht verhoogd. Een kleine rotatiestraf werd toegevoegd om aanhoudend ronddraaiend gedrag bij de prey te ontmoedigen. Dit geeft de hunter een realistische kans om de prey te vangen, aangezien de hunter iets trager beweegt.

**Observaties:** Rond 30.000 stappen vertonen beide agents een herkenbaar doel gedrag. De leercurve vlakt snel af en stabiliseert rond een reward van 0 tot -1. Dit wijst erop dat de agents het gewenste basisgedrag hebben aangeleerd, hoewel de beloningsbalans nog niet optimaal is.

<img width="314" height="204" alt="image" src="https://github.com/user-attachments/assets/e3b6bf58-b8f5-4806-af2f-15bef79448b6" />

<img width="222" height="55" alt="image" src="https://github.com/user-attachments/assets/219b3baa-e41e-40a7-8ef9-58377a7908ba" />

De plateau-waarde van de gecumuleerde beloningen ligt rond 0 tot -1, wat suggereert dat er nog ruimte is voor verdere optimalisatie van de reward-balans. Het gedrag tijdens de training is te bekijken in de volgende demonstratievideo (opgenomen rond stap 300.000):

[Trainingsdemonstratie — YouTube](https://youtu.be/tAOeAy_W2IQ)

> **Visuele indicatie:** De vloerafbeelding verandert van kleur afhankelijk van de winnaar van de episode — groen bij een overwinning van de prey, geel bij een overwinning van de hunter. Dit biedt een snelle visuele feedback tijdens het trainingsproces.
> 

## Conclusie

Over drie iteraties is een progressieve verbetering van het leergedrag van beide agents waargenomen. De voornaamste bevinding is dat het gebruik van relatieve positionele observaties (`InverseTransformPoint`) de convergentie van de hunter aanzienlijk versnelt in vergelijking met absolute coördinaten.

Het lijkt erop dat de grootste uitdaging niet de hunter betreft, maar de prey: het balanceren van overleven en fruit verzamelen vereist een nauwkeurige reward-structuur waarbij positieve afstandsgebaseerde rewards worden vermeden, aangezien deze aanleiding geven tot circulair gedrag. Een strafbenadering op basis van afstand tot het dichtstbijzijnde fruit blijkt robuuster.

De huidige reward-waarden (stabiele plateau rond 0 tot -1) wijzen erop dat het systeem functioneel is maar nog niet optimaal gebalanceerd. Verdere training met verbeterede reward-structuur, en meerdere herhalingen van het experiment zouden meer zekerheid kunnen geven over de generaliseerbaarheid van deze bevindingen.

## Referenties

Jason Builds, Youtube. *Hunter v. Prey Machine Learning — Pellet Grabber MLAgents Unity Tutorial #7.* https://www.youtube.com/watch?v=KHgSDFB9nTE (gebruikt als inspiratie en basis)
