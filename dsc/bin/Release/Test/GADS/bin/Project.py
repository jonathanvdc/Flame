class IReadOnlyCollection:
    """ Describes a generic read-only collection of items. """

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        raise NotImplementedError("Getter of property 'IReadOnlyCollection.count' was not implemented.")


class ICollection(IReadOnlyCollection):
    """ Describes a collection that allows items to be added. """

    def add(self, Item):
        """ Adds an item to the collection. """
        raise NotImplementedError("Method 'ICollection.add' was not implemented.")


class IReadOnlyList(IReadOnlyCollection):
    """ Describes a generic read-only list. """

    def __getitem__(self, Index):
        """ Gets the item at the specified position in the list. """
        raise NotImplementedError("Method 'IReadOnlyList.__getitem__' was not implemented.")


class IList(IReadOnlyList, ICollection):
    """ Describes a generic list. """

    def insert(self, Index, Item):
        """ Inserts an item in the list at the specified position. """
        raise NotImplementedError("Method 'IList.insert' was not implemented.")

    def remove_at(self, Index):
        """ Removes the element at the specified index from the list. """
        raise NotImplementedError("Method 'IList.remove_at' was not implemented.")

    def __getitem__(self, Index):
        """ Gets the item at the specified position in the list. """
        raise NotImplementedError("Method 'IList.__getitem__' was not implemented.")

    def __setitem__(self, Index, value):
        """ Sets the item at the specified position in the list. """
        raise NotImplementedError("Method 'IList.__setitem__' was not implemented.")


class IRecord:
    """ Describes a generic record. """

    @property
    def key(self):
        """ Gets the record's search key. """
        raise NotImplementedError("Getter of property 'IRecord.key' was not implemented.")


class IMap:
    """ Describes a map, an object that maps source values to their target representation. It is essentially a pure mathematical function. """

    def map(self, Item):
        """ Maps the item to its target representation. """
        raise NotImplementedError("Method 'IMap.map' was not implemented.")


class IDictionary:
    """ Represents a generic dictionary, or table. """

    def to_list(self):
        """ Gets the contents of this dictionary as a list. """
        raise NotImplementedError("Method 'IDictionary.to_list' was not implemented.")

    def remove(self, Key):
        """ Removes a key from the dictionary. """
        raise NotImplementedError("Method 'IDictionary.remove' was not implemented.")

    def contains_key(self, Key):
        """ Finds out of this dictionary contains the specified key. """
        raise NotImplementedError("Method 'IDictionary.contains_key' was not implemented.")

    def __getitem__(self, Key):
        """ Gets the item associated with the specified key in the dictionary. """
        raise NotImplementedError("Method 'IDictionary.__getitem__' was not implemented.")

    def __setitem__(self, Key, value):
        """ Sets the item associated with the specified key in the dictionary. """
        raise NotImplementedError("Method 'IDictionary.__setitem__' was not implemented.")

    @property
    def keys(self):
        """ Gets all keys in this table. """
        raise NotImplementedError("Getter of property 'IDictionary.keys' was not implemented.")

    @property
    def values(self):
        """ Gets all values in this table. """
        raise NotImplementedError("Getter of property 'IDictionary.values' was not implemented.")


class ISortedList(ICollection):
    """ Describes a generic sorted list. """

    def remove(self, Item):
        """ Removes an item from a list. """
        raise NotImplementedError("Method 'ISortedList.remove' was not implemented.")

    def contains(self, Item):
        """ Finds out if the sorted list contains the given item. """
        raise NotImplementedError("Method 'ISortedList.contains' was not implemented.")

    def to_list(self):
        """ Returns a read-only list that represents this list's contents, for easy enumeration. """
        raise NotImplementedError("Method 'ISortedList.to_list' was not implemented.")


class ArrayList(IList):
    """ An array-based implementation of a list. """

    def __init__(self):
        """ Creates a new instance of a list. """
        self.data = [None] * 5
        self.elem_count = 0

    def add(self, Item):
        """ Adds an item to the end of the list. """
        if not self.count < len(self.data):
            newData = [None] * (len(self.data) + 5)
            self.copy_to(newData)
            self.data = newData
        self.data[self.elem_count] = Item
        self.elem_count += 1

    def copy_to(self, Target):
        list = self.data
        i = len(list)
        j = 0
        k = len(Target)
        while j < i and j < k:
            Target[j] = list[j]
            j += 1

    def insert(self, Index, Item):
        """ Inserts an item in the list at the specified position. """
        if Index == self.count:
            self.add(Item)
            return True
        else:
            if Index < self.count:
                self.shift_right(Index)
                self.data[Index] = Item
                self.elem_count += 1
                return True
            else:
                return False

    def shift_right(self, StartIndex):
        if not self.count < len(self.data):
            newData = [None] * (len(self.data) + 5)
            i = 0
            while i < StartIndex:
                newData[i] = self.data[i]
                i += 1
            i = StartIndex
            while i < self.count:
                newData[i + 1] = self.data[i]
                i += 1
            self.data = newData
        else:
            i = self.count - 1
            while not i < StartIndex:
                self.data[i + 1] = self.data[i]
                i -= 1

    def remove_at(self, Index):
        """ Removes the element at the specified index from the list. """
        if (not Index < 0) and Index < self.count:
            self.shift_left(Index)
            self.elem_count -= 1
            return True
        else:
            return False

    def shift_left(self, StartIndex):
        i = StartIndex + 1
        while i < self.count:
            self.data[i - 1] = self.data[i]
            i += 1

    def contains(self, Item):
        i = 0
        while i < self.count:
            if self.data[i] == Item:
                return True
            i += 1
        return False

    def to_array(self):
        arr = [None] * self.count
        self.copy_to(arr)
        return arr

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        return self.elem_count

    def __getitem__(self, Index):
        """ Gets the item in the list at the specified position. """
        return self.data[Index]

    def __setitem__(self, Index, value):
        """ Sets the item in the list at the specified position. """
        self.data[Index] = value


class ListNode:

    def __init__(self, Value):
        self.value_value = None
        self.successor_value = None
        self.value = Value

    def insert_after(self, Value):
        nextVal = ListNode(Value)
        nextVal.successor = self.successor
        self.successor = nextVal

    @property
    def value(self):
        return self.value_value

    @value.setter
    def value(self, value):
        self.value_value = value

    @property
    def successor(self):
        return self.successor_value

    @successor.setter
    def successor(self, value):
        self.successor_value = value

    @property
    def tail(self):
        if self.successor == None:
            return self
        else:
            return self.successor.tail


class LinkedList(IList):

    def __init__(self):
        self.head_value = None

    def to_array(self):
        arr = [None] * self.count
        node = self.head
        i = 0
        while i < len(arr):
            arr[i] = node.value
            node = node.successor
            i += 1
        return arr

    def node_at(self, Index):
        node = self.head
        i = 0
        while node != None and i < Index:
            node = node.successor
            i += 1
        return node

    def item_at(self, Index):
        return self.node_at(Index).value

    def add(self, Item):
        """ Adds an item to the collection. """
        if self.head == None:
            self.head = ListNode(Item)
        else:
            self.tail.insert_after(Item)

    def remove_at(self, Index):
        """ Removes the element at the specified index from the list. """
        if Index < self.count:
            if Index == 0:
                self.head = self.head.successor
            else:
                predecessor = self.node_at(Index - 1)
                predecessor.successor = predecessor.successor.successor
            return True
        else:
            return False

    def insert(self, Index, Item):
        """ Inserts an item in the list at the specified position. """
        if Index == 0:
            oldHead = self.head
            self.head = ListNode(Item)
            if oldHead != None:
                self.head.successor = oldHead
            return True
        node = self.node_at(Index - 1)
        if node == None:
            return False
        else:
            node.insert_after(Item)
            return True

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        node = self.head
        i = 0
        while node != None:
            node = node.successor
            i += 1
        return i

    @property
    def head(self):
        return self.head_value

    @head.setter
    def head(self, value):
        self.head_value = value

    @property
    def tail(self):
        if self.head == None:
            return None
        else:
            return self.head.tail

    def __getitem__(self, Index):
        """ Gets the item at the specified position in the list. """
        return self.item_at(Index)

    def __setitem__(self, Index, value):
        """ Sets the item at the specified position in the list. """
        self.node_at(Index).value = value


class Stack(IReadOnlyCollection):
    """ Represents a generic stack. """

    def __init__(self, dataContainer = None):
        if dataContainer == None:
            self.data_container = None
            self.data_container = LinkedList()
            return
        self.data_container = None
        self.data_container = dataContainer

    def push(self, Item):
        """ Pushes an item on the stack. """
        self.data_container.insert(0, Item)

    def peek(self):
        """ Peeks at the item at the top of the stack, without removing it. """
        return self.data_container[0]

    def pop(self):
        """ Pops the item at the top of the stack. """
        value = self.data_container[0]
        self.data_container.remove_at(0)
        return value

    @property
    def count(self):
        """ Gets the number of items on the stack. """
        return self.data_container.count


class BinaryTree:

    def __init__(self, Value = None, Left = None, Right = None):
        if Value == None and Left == None and Right == None:
            self.value_value = None
            self.left_value = None
            self.right_value = None
            return
        if Left == None and Right == None:
            self.value_value = None
            self.left_value = None
            self.right_value = None
            self.value = Value
            return
        self.value_value = None
        self.left_value = None
        self.right_value = None
        self.value = Value
        self.left = Left
        self.right = Right

    def traverse_inorder(self, Target = None):
        if Target == None:
            list = ArrayList()
            self.traverse_inorder(list)
            return list
        if self.left != None:
            self.left.traverse_inorder(Target)
        Target.add(self.value)
        if self.right != None:
            self.right.traverse_inorder(Target)

    def retrieve_and_remove_inorder_successor(self):
        if self.right.left == None:
            val = self.right.value
            self.right = self.right.right
            return val
        else:
            return self.right.retrieve_and_remove_leftmost()

    def retrieve_and_remove_leftmost(self):
        if self.left.left == None:
            val = self.left.value
            self.left = self.left.right
            return val
        else:
            return self.left.retrieve_and_remove_leftmost()

    def insert(self, Item):
        if Item < self.value:
            if self.left == None:
                self.left = BinaryTree(Item)
            else:
                self.left.insert(Item)
        else:
            if self.right == None:
                self.right = BinaryTree(Item)
            else:
                self.right.insert(Item)

    def contains(self, Item):
        if Item == self.value:
            return True
        else:
            if Item < self.value:
                if self.left != None:
                    return self.left.contains(Item)
                else:
                    return False
            else:
                if self.right != None:
                    return self.right.contains(Item)
                else:
                    return False

    def retrieve(self, Item):
        if Item == self.value:
            return self.value
        else:
            if Item < self.value:
                if self.left != None:
                    return self.left.retrieve(Item)
                else:
                    return None
            else:
                if self.right != None:
                    return self.right.retrieve(Item)
                else:
                    return None

    def remove(self, Item):
        if Item == self.value:
            if self.right != None:
                self.value = self.retrieve_and_remove_inorder_successor()
            else:
                self.value = self.left.value
                self.right = self.left.right
                self.left = self.left.left
        else:
            if Item < self.value:
                self.left.remove(Item)
            else:
                self.right.remove(Item)

    @property
    def right(self):
        return self.right_value

    @right.setter
    def right(self, value):
        self.right_value = value

    @property
    def left(self):
        return self.left_value

    @left.setter
    def left(self, value):
        self.left_value = value

    @property
    def value(self):
        return self.value_value

    @value.setter
    def value(self, value):
        self.value_value = value

    @property
    def is_leaf(self):
        return self.left == None and self.right == None

    @property
    def leftmost_tree(self):
        if self.left == None:
            return self
        else:
            return self.left.leftmost_tree

    @property
    def rightmost_tree(self):
        if self.right == None:
            return self
        else:
            return self.right.rightmost_tree


class KeyValuePair(IRecord):
    """ Describes a key-value pair. """

    def __init__(self, Key, Value):
        """ Creates a new key-value pair. """
        self.key_value = None
        self.value_value = None
        self.key = Key
        self.value = Value

    def __gt__(self, Other):
        return self.key > Other.key

    def __ge__(self, Other):
        return not self.key < Other.key

    def __lt__(self, Other):
        return self.key < Other.key

    def __le__(self, Other):
        return not self.key > Other.key

    def __eq__(self, Other):
        if Other == None:
            return False
        return self.key == Other.key

    def __ne__(self, Other):
        if Other == None:
            return True
        return self.key != Other.key

    def __str__(self):
        return "(" + str(self.key) + ", " + str(self.value) + ")"

    def __repr__(self):
        return self.__str__()

    @property
    def key(self):
        """ Gets the key-value pair's key. """
        return self.key_value

    @key.setter
    def key(self, value):
        """ Sets the key-value pair's key. """
        self.key_value = value

    @property
    def value(self):
        """ Gets the key-value pair's value. """
        return self.value_value

    @value.setter
    def value(self, value):
        """ Sets the key-value pair's value. """
        self.value_value = value


class BinaryTreeTable(IDictionary):
    """ Represents a binary tree implementation of a table. """

    def __init__(self):
        self.tree = None

    def insert(self, Key, Value):
        """ Inserts a new item into the binary tree table. """
        pair = KeyValuePair(Key, Value)
        if self.tree == None:
            self.tree = BinaryTree(pair)
        else:
            self.tree.insert(pair)

    def contains_key(self, Key):
        """ Finds out of this table contains the specified key. """
        return self.tree != None and self.tree.contains(KeyValuePair(Key, None))

    def remove(self, Key):
        """ Removes a key from the table. """
        if self.contains_key(Key):
            pair = KeyValuePair(Key, None)
            self.tree.remove(pair)
            if self.tree.value == None:
                self.tree = None
            return True
        else:
            return False

    def to_list(self):
        """ Gets the contents of this table as a list. """
        if self.tree == None:
            return ArrayList()
        return self.tree.traverse_inorder()

    def __getitem__(self, Key):
        """ Gets the value associated with a key. """
        if self.tree == None:
            return None
        return self.tree.retrieve(KeyValuePair(Key, None)).value

    def __setitem__(self, Key, value):
        """ Sets the value associated with a key. """
        self.remove(Key)
        self.insert(Key, value)

    @property
    def keys(self):
        """ Gets all keys in this table. """
        items = self.to_list()
        keys = ArrayList()
        i = 0
        while i < items.count:
            keys.add(items[i].key)
            i += 1
        return keys

    @property
    def values(self):
        """ Gets all keys in this table. """
        items = self.to_list()
        vals = ArrayList()
        i = 0
        while i < items.count:
            vals.add(items[i].value)
            i += 1
        return vals


class DictionarySortedList(ISortedList):
    """ Represents a list that sorts its contents through a dictionary. """

    def __init__(self, Dictionary, KeyMap):
        self.dictionary = None
        self.key_map_value = None
        self.dictionary = Dictionary
        self.key_map = KeyMap

    def add(self, Item):
        """ Adds an item to the collection. """
        self.dictionary[self.key_map.map(Item)] = Item

    def remove(self, Item):
        """ Removes an item from a list. """
        return Boolean(self.dictionary.remove(self.key_map.map(Item)))

    def contains(self, Item):
        """ Finds out if the sorted list contains the given item. """
        return self.dictionary.contains_key(self.key_map.map(Item))

    def to_list(self):
        """ Returns a read-only list that represents this list's contents, for easy enumeration. """
        return self.dictionary.values

    @property
    def key_map(self):
        """ Gets the mapping function that maps the records to their respective keys. """
        return self.key_map_value

    @key_map.setter
    def key_map(self, value):
        """ Sets the mapping function that maps the records to their respective keys. """
        self.key_map_value = value

    @property
    def count(self):
        """ Gets the number of items in the sorted list. """
        return self.dictionary.keys.count


class Date:
    """ Represents a simple date. """

    def __init__(self, Day, Month, Year):
        """ Creates a new date from a day, month and year. """
        self.day_value = 0
        self.month_value = 0
        self.year_value = 0
        self.day = Day
        self.month = Month
        self.year = Year

    def __str__(self):
        """ Gets this date's string representation. """
        return str(self.day) + "/" + str(self.month) + "/" + str(self.year)

    @property
    def day(self):
        """ Gets the day of this date. """
        return self.day_value

    @day.setter
    def day(self, value):
        """ Sets the day of this date. """
        self.day_value = value

    @property
    def month(self):
        """ Gets the month of this date. """
        return self.month_value

    @month.setter
    def month(self, value):
        """ Sets the month of this date. """
        self.month_value = value

    @property
    def year(self):
        """ Gets the year of this date. """
        return self.year_value

    @year.setter
    def year(self, value):
        """ Sets the year of this date. """
        self.year_value = value


class Time:
    """ Represents the time of day. """

    def __init__(self, Hour, Minute, Second = None):
        """ Creates a new time based on an hour, minute and second. """
        if Second == None:
            self.total_seconds = 0
            self.total_seconds = Hour * 3600 + Minute * 60
            return
        self.total_seconds = 0
        self.total_seconds = Hour * 3600 + Minute * 60 + Second

    def __str__(self):
        """ Gets the time's string representation. """
        if self.second == 0:
            return str(self.hour) + ":" + str(self.minute)
        else:
            return str(self.hour) + ":" + str(self.minute) + ":" + str(self.second)

    @property
    def second(self):
        """ Gets the second of this time instance. """
        return self.total_seconds % 60

    @property
    def hour(self):
        """ Gets the hour of this time instance. """
        return self.total_seconds // 3600

    @property
    def minute(self):
        """ Gets the minute of this time instance. """
        return (self.total_seconds % 3600) // 60


class DateTime:
    """ Describes a date and time: a date and the time of day. """

    def __init__(self, Date, TimeOfDay):
        """ Creates a new date-time instance based on the date and time provided. """
        self.date_value = None
        self.time_of_day_value = None

    def __str__(self):
        """ Gets the time's string representation. """
        return str(self.date) + " " + str(self.time_of_day)

    @property
    def date(self):
        """ Gets this timestamp's date. """
        return self.date_value

    @date.setter
    def date(self, value):
        """ Sets this timestamp's date. """
        self.date_value = value

    @property
    def time_of_day(self):
        """ Gets this timestamp's time of day. """
        return self.time_of_day_value

    @time_of_day.setter
    def time_of_day(self, value):
        """ Sets this timestamp's time of day. """
        self.time_of_day_value = value


class Theater:
    """ Represents a movie theater. """

    def __init__(self, Name):
        """ Creates a new movie theater instance. """
        self.auditors = ArrayList()
        self.slots = ArrayList()
        self.allmovies = ArrayList()
        self.name_value = None
        self.name = Name

    def build_auditorium(self, NumberOfSeats):
        """ Builds and returns a new auditorium with the specified number of seats. """
        auditor = Auditorium(self.auditors.count, NumberOfSeats)
        self.auditors.add(auditor)
        return auditor

    @property
    def name(self):
        """ Gets the movie theater's name. """
        return self.name_value

    @name.setter
    def name(self, value):
        """ Sets the movie theater's name. """
        self.name_value = value

    @property
    def auditoria(self):
        """ Gets a read-only list of all auditoria in this movie theater. """
        return self.auditors

    @property
    def timeslots(self):
        """ Gets the list of all available timeslots for this movie theater. """
        return self.slots

    @property
    def movies(self):
        """ Gets the list of all movies known to the movie theater. """
        return self.allmovies


class Auditorium(IRecord):
    """ Describes an auditorium. """

    def __init__(self, Index, NumberOfSeats):
        self.index_value = 0
        self.number_of_seats_value = 0
        self.index = Index
        self.number_of_seats = NumberOfSeats

    def __str__(self):
        return "Auditorium " + str(self.index) + "(" + str(self.number_of_seats) + " seats)"

    @property
    def index(self):
        """ Gets the auditorium's index, or room number. """
        return self.index_value

    @index.setter
    def index(self, value):
        """ Sets the auditorium's index, or room number. """
        self.index_value = value

    @property
    def number_of_seats(self):
        """ Gets the number of seats in the auditorium. """
        return self.number_of_seats_value

    @number_of_seats.setter
    def number_of_seats(self, value):
        """ Sets the number of seats in the auditorium. """
        self.number_of_seats_value = value

    @property
    def key(self):
        """ Gets the record's search key. """
        return self.index


class Movie(IRecord):
    """ Describes a movie. """

    def __init__(self, Id, Title, Rating):
        self.id_value = 0
        self.title_value = None
        self.rating_value = 0.0
        self.id = Id
        self.title = Title
        self.rating = Rating

    def __str__(self):
        return str(self.id) + ": " + self.title + "(Rated " + str(self.rating) + ")"

    @property
    def id(self):
        """ Gets the movie's identifier. """
        return self.id_value

    @id.setter
    def id(self, value):
        """ Sets the movie's identifier. """
        self.id_value = value

    @property
    def title(self):
        """ Gets the movie's title. """
        return self.title_value

    @title.setter
    def title(self, value):
        """ Sets the movie's title. """
        self.title_value = value

    @property
    def rating(self):
        """ Gets the movie's rating. """
        return self.rating_value

    @rating.setter
    def rating(self, value):
        """ Sets the movie's rating. """
        self.rating_value = value

    @property
    def key(self):
        """ Gets the record's search key. """
        return self.id


class Showtime(IRecord):
    """ Describes a showtime at the movie theater. """

    def __init__(self, Id, Location, MoviePlaying, Timeslot):
        self.id_value = 0
        self.location_value = None
        self.movie_playing_value = None
        self.timeslot_value = None
        self.number_of_free_seats_value = 0
        self.id = Id
        self.location = Location
        self.movie_playing = MoviePlaying
        self.timeslot = Timeslot

    def __str__(self):
        return "Showtime " + str(self.id) + " of " + str(self.movie_playing) + ", " + str(self.location) + ", at " + str(self.timeslot) + ", " + str(self.number_of_free_seats) + " free seats"

    @property
    def id(self):
        return self.id_value

    @id.setter
    def id(self, value):
        self.id_value = value

    @property
    def movie_playing(self):
        return self.movie_playing_value

    @movie_playing.setter
    def movie_playing(self, value):
        self.movie_playing_value = value

    @property
    def location(self):
        return self.location_value

    @location.setter
    def location(self, value):
        self.location_value = value

    @property
    def timeslot(self):
        return self.timeslot_value

    @timeslot.setter
    def timeslot(self, value):
        self.timeslot_value = value

    @property
    def number_of_free_seats(self):
        return self.number_of_free_seats_value

    @number_of_free_seats.setter
    def number_of_free_seats(self, value):
        self.number_of_free_seats_value = value

    @property
    def auditorium_index(self):
        return self.location.index

    @property
    def movie_id(self):
        return self.movie_playing.id

    @property
    def key(self):
        """ Gets the record's search key. """
        return self.id


class User(IRecord):
    """ Describes a registered customer at a movie theater. """

    def __init__(self, Id, FirstName, LastName, EmailAddress):
        """ Creates a new instance of a user with the provided information. """
        self.id_value = 0
        self.first_name_value = None
        self.last_name_value = None
        self.email_address_value = None
        self.id = Id
        self.first_name = FirstName
        self.last_name = LastName
        self.email_address = EmailAddress

    def __str__(self):
        """ Gets the user's data as a string. """
        return str(self.id) + ": " + self.name + "(" + self.email_address + ")"

    @property
    def id(self):
        """ Gets the user's unique identifier. """
        return self.id_value

    @id.setter
    def id(self, value):
        """ Sets the user's unique identifier. """
        self.id_value = value

    @property
    def name(self):
        """ Gets the user's full name. """
        return self.first_name + " " + self.last_name

    @property
    def first_name(self):
        """ Gets the user's first name. """
        return self.first_name_value

    @first_name.setter
    def first_name(self, value):
        """ Sets the user's first name. """
        self.first_name_value = value

    @property
    def last_name(self):
        """ Gets the user's last name. """
        return self.last_name_value

    @last_name.setter
    def last_name(self, value):
        """ Sets the user's last name. """
        self.last_name_value = value

    @property
    def email_address(self):
        """ Gets the user's email address. """
        return self.email_address_value

    @email_address.setter
    def email_address(self, value):
        """ Sets the user's email address. """
        self.email_address_value = value

    @property
    def key(self):
        """ Gets the record's search key. """
        return self.id


class Reservation(IRecord):
    """ Describes a reservation at a movie theater by a registered customer. """

    def __init__(self, Id, Customer, Showtime, Timestamp, NumberOfSeats):
        self.id_value = 0
        self.customer_value = None
        self.timestamp_value = None
        self.showtime_value = None
        self.number_of_seats_value = 0
        self.id = Id
        self.customer = Customer
        self.showtime = Showtime
        self.timestamp = Timestamp
        self.number_of_seats = NumberOfSeats

    @property
    def id(self):
        """ Gets the reservation's unique identifier. """
        return self.id_value

    @id.setter
    def id(self, value):
        """ Sets the reservation's unique identifier. """
        self.id_value = value

    @property
    def customer(self):
        """ Gets the customer that has reserved a seat. """
        return self.customer_value

    @customer.setter
    def customer(self, value):
        """ Sets the customer that has reserved a seat. """
        self.customer_value = value

    @property
    def showtime(self):
        """ Gets the showtime associated with this reservation. """
        return self.showtime_value

    @showtime.setter
    def showtime(self, value):
        """ Sets the showtime associated with this reservation. """
        self.showtime_value = value

    @property
    def timestamp(self):
        """ Gets the date and time at which the reservation was placed. """
        return self.timestamp_value

    @timestamp.setter
    def timestamp(self, value):
        """ Sets the date and time at which the reservation was placed. """
        self.timestamp_value = value

    @property
    def number_of_seats(self):
        """ Gets the number of seats reserved by the user. """
        return self.number_of_seats_value

    @number_of_seats.setter
    def number_of_seats(self, value):
        """ Sets the number of seats reserved by the user. """
        self.number_of_seats_value = value

    @property
    def key(self):
        """ Gets the record's search key. """
        return self.id


class Ticket(IRecord):
    """ Describes a ticket at a movie theater. """

    def __init__(self, Customer):
        self.customer_value = None
        self.customer = Customer

    @property
    def customer(self):
        """ Gets the user that is associated with this ticket. """
        return self.customer_value

    @customer.setter
    def customer(self, value):
        """ Sets the user that is associated with this ticket. """
        self.customer_value = value

    @property
    def customer_id(self):
        """ Gets the identifier of the user that is associated with this ticket. """
        return self.customer.id

    @property
    def key(self):
        """ Gets the record's search key. """
        return self.customer_id