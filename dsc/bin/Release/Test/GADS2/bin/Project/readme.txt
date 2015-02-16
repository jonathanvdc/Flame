Readme Contracts
--------------------------
Groep B
--------------------------
Olivier Bloch

Jonathan Van der Cruysse

Othman Nahhas

Sibert Aerts 
--------------------------

De meeste contracten werden geselecteerd uit het voorstel van Jonathan, omwille van de relatieve compleetheid ervan.
De volgende aanpassingen werden gemaakt: 
 - Elke implementatie 'IReadOnlyCollection<T>' dient ook 'iterable<T>' te implementeren, 
   zodat deze gebruikt kan worden als verzameling in een for-loop.
 - Elke implementatie 'ITree<T, TKey>' dient ook 'iterable<T>' te implementeren, 
   zodat tabellen en gesorteerde lijsten die dit gebruiken op deze functionaliteit kunnen steunen voor hun eigen implementatie van 'iterable<T>'.
 - Er zijn enkele klassen toegevoegd ter ondersteuning van dubbelgelinkte lijsten, namelijk 'DoublyLinkedList<T>' en 'DoubleNode<T>'.
   'DoublyLinkedList<T>' staat geen low-level access toe tot zijn nodes zoals de 'LinkedList<T>', om de integriteit van de lijst te behouden.
 - Kleine veranderingen aan de beschrijvingen van de klassen en ADTs.