==========================
Jonathan Van der Cruysse
Informatica Ba 1
s0142476
=========================
Implementatie hashmap
==========================
Tests door: Olivier Bloch
==========================
Intro:
  De contracten die hier opgesteld zijn ofwel de hashmaps, ofwel ADTs die de hashmaps direct of indirect gebruiken.
  Een heel aantal contracten die hier opgesteld zijn overlappen dus met de contracten uit de vorige opdracht.
  Ze zijn niet of weinig aangepast, maar wel toegevoegd om gemakkelijk de overeenkomst met de rest van het ontwerp te kunnen bestuderen.

Overeenkomst met opgave:
  De gewenste tabel die kan wisselen tussen linear probing, quadratic probing en separate chaining is de 'SwapTable'.
  Via de methode 'Swap' kan een andere onderliggende tabel, zoals een 'Hashtable' (seperate chaining) of een 'OpenHashtable' (open addressing), 
  aan toegewezen worden. 'Hashtable', de tabel die separate chaining gebruikt,
  kan een andere vorm van bucket krijgen door een andere 'bucket factory', zoals een 'BinaryTreeTableFactory': 
  een object dat buckets aanmaakt op basis van een value-key mapping functie. Deze buckets zijn zelf ook tabelimplementaties.
  'OpenHashtable', die separate chaining gebruikt, kan op analoge wijze voorzien worden van een 'probe sequence map': 
  een mapping functie die een gegeven hashcode mapt naar een 'generator', die de zoeksequentie in de tabel voorstelt,
  zonder tegemoet te komen aan de tabelgrootte. Dit is immers de verantwoordelijkheid van de tabel zelf.
  Een voorbeeld van zo'n 'probe sequence map' is de 'PowerSequenceMap', die via een parameter in de constructor kan geconfigureerd worden om
  een lineaire, kwadratische, kubieke... reeks te genereren op basis van een getal, dat als eerste getal van de reeks geldt.
  Op te merken valt dat dit ontwerp alleen maar een een 'BinaryTreeTableFactory' klasse bevat die een instantieerbare 'bucket factory' is,
  dit is opzettelijk: 'bucket factories' zijn erg kleine klassen die gewoonlijk slechts de constructor van een andere klasse aanroepen,
  het is verder ook aannemelijk dat de client zijn eigen soort 'bucket factories' maakt en deze aan de 'Hashtable' in de constructor doorgeeft.
  Om al die redenen acht ik het bijvoegen van een aparte 'bucket factory' voor elke vorm van tabel overbodig.