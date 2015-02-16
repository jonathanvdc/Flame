from ISortableTable import *

class ListTable(ISortableTable):
    """ An implementation of a table that uses a list as backing storage. """

    def __init__(self, KeyMap, List):
        """ Creates a new list table instance. """
        self.key_map = KeyMap
        self.list = List

    def contains_key(self, Key):
        """ Finds out if the table contains the specified key. """
        for item in self.list:
            if self.key_map.map(item) == Key:
                return True
        return False

    def remove(self, Key):
        """ Removes a key from the table. """
        # Post:
        # This method returns true if the key is in the table, false if not.
        i = 0
        while i < self.list.count:
            if self.key_map.map(self.list[i]) == Key:
                self.list.remove_at(i)
                return True
            i += 1
        return False

    def insert(self, Item):
        """ Inserts an item into the table. """
        # Post:
        # Returns true if item is successfully inserted, false if the table already contains an item with the same search key.
        if self.contains_key(self.key_map.map(Item)):
            return False
        else:
            self.list.add(Item)
            return True

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        return self.list.__iter__()

    def to_list(self):
        """ Gets the table's items as a read-only list.
            The elements in this list are in the same order as those in the table's iterator, obtained through '__iter__' (the get iterator method).
            Any statement that applies to this method therefore also applies to the '__iter__' (get iterator) method, and vice-versa. """
        # Post:
        # This method returns a read-only list that describes the items in this table.
        # Modifications to this list are not allowed - it is read-only.
        # Furthermore, this list may be an alias to an internal list containing the table's items, or a copy.
        # This list need not be sorted, but must contain every item in the table.
        return self.list

    def sort(self, Sorter):
        """ Sorts the list based on the given item comparer. """
        # Pre:
        # The table must not be empty for this method to change the table's state.
        # Post:
        # The table's items will be sorted.
        # After sorting, the table's 'ToList()' method must return a list whose items are sorted.
        # If the table's state is modified after 'Sort()' is called, the list produced by 'ToList()' need no longer be sorted.
        self.list = Sorter.sort(self.list)

    @property
    def key_map(self):
        """ Gets the value-to-key mapping function of this list table. """
        return self.key_map_value

    @key_map.setter
    def key_map(self, value):
        """ Sets the value-to-key mapping function of this list table.
            This accessor is private. """
        self.key_map_value = value

    def __getitem__(self, Key):
        """ Retrieves the item in the table with the specified key. """
        # Pre:
        # For this method to return an item in the table, rather than null, the key must be in the table, i.e.
        # ContainsKey(Key) must return true.
        # Post:
        # The return value of this method will be the item that corresponds with the key, or None, if it is not found.
        # It is recommended to check if the table contains the key by using ContainsKey.
        for item in self.list:
            if self.key_map.map(item) == Key:
                return item
        return None

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        return self.list.count