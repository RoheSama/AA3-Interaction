# AA3-Interaction

Members:

Roger Companys Solà
roger.companys@enti.cat

Eva Portet Tuñon
eva.portet@enti.cat

Exercicies location:

1.1 - IK_Tentacles.cs i MyTentacleController, dins del dll d’Octopus Controller, que hem modificat per aquesta pràctica.
1.2 - MovingTarget.cs (el que es mou es el target blau)
1.3 - Al UI_Controller.cs, es controla l’slider i a IK_Scorpion StartShootBall().
1.4 -  Al script moving_ball.cs, UpdateInputs()

2.1 -
2.2 - Es calcula l’acceleració després de cada fotograma, composta per la força de gravetat i el nostre Magnus Effect. La nova força iguala el producte transversal de la velocitat angular (constant) i la velocitat lineal instantània de cada fotograma.
MagnusForce = Cross(AngularVelocity, InstantLinearVelocity);
Hem triat aquesta formula, per què en aquesta pràctica no es necessari tenir en compte la densitat de l’aire, la fricció o altres paràmetres que utilitzariam en jocs més complerts i complexos.
2.3 - 
2.4 - A MovingBall.cs, UpdateInputs(), mira l’input de I i activa o desactiva la informació de les fletxes.

3.1 - Dins de IK_Scorpion.cs, a la funció UpdateLegsAndBody().
3.2 - A la pròpia escena
3.3 - Al dll, al script MyScorpionController, modificat per aquesta pràctica, a la funció ComputeBaseBonePosition().
3.4. - De nou a la funció UpdateLegsAndBody(), dins de IK_Scorpion.cs, ara tindrem en compte també la posició de MainBody.
3.5 - A la funció UpdateLegsAndBody(), tingut en compte la posició de les cames de l’esquerra i les dretes per separat per fer-ho.
3.6 - Dins de IK_Scorpion.cs, a la funció RotateBody().

4.1 - A la funció SetTailLearningRate(), de IK_Scorpion.cs
4.2 - A una nova funció dins de MyScorpionController, al dll, anomenada DistanceFromTargetAndOrientation().


5.1 - A una nova funció creada dins de MyOctopusController, al dll, ClampBoneRotation().
5.2 -  De nou dins de MyOctopusController, dins de la funció  update_ccd(), ho hem fet utilitzant la funció Lerp().
5.3 - Hem donat a cada tentacle bone, uns valors que oscil·lessin entre: Vector3(-20, 0, -3) i Vector3(20, 0, 3).

Problemes:

Un cop completat el primer xut després de l'execució, tenim un problema amb el Reset de la cua que fa que el FABRIK canvii els parametres dels joints que la conformin i aquesta es deforma constantment de formes no desitjades. 
El reset de la pilota tampoc es completa correctament quan volem iniciar un nou xut.


