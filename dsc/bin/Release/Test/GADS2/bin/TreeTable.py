from ITable import *

class TreeTable(ITable):
    """ A search tree implementation of a table. """
    # Remarks:
    # TKey is the type of keys stored in the table, TValue is the type of values.

    def __init__(self, tree):
        """ Creates a new tree implementation of a table, using the provided tree as backing storage. """
        self.tree = tree

    def insert(self, Item):
        """ Inserts an item into the table. """
        # Post:
        # Returns true if item is successfully inserted, false if the table already contains an item with the same search key.
        if not self.contains_key(self.key_map.map(Item)):
            self.tree.insert(Item)
            return True
        else:
            return False

    def contains_key(self, Key):
        """ Finds out if the table contains the specified key. """
        return self[Key] is not None

    def remove(self, Key):
        """ Removes a key from the table. """
        # Post:
        # This method returns true if the key is in the table, false if not.
        return self.tree.remove(Key)

    def to_list(self):
        """ Gets the table's items as a read-only list.
            The elements in this list are in the same order as those in the table's iterator, obtained through '__iter__' (the get iterator method).
            Any statement that applies to this method therefore also applies to the '__iter__' (get iterator) method, and vice-versa. """
        # Post:
        # This method returns a read-only list that describes the items in this table.
        # Modifications to this list are not allowed - it is read-only.
        # Furthermore, this list may be an alias to an internal list containing the table's items, or a copy.
        # This list need not be sorted, but must contain every item in the table.
        return self.tree.traverse_inorder()

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        return self.tree.__iter__()

    def __getitem__(self, Key):
        """ Retrieves the item in the table with the specified key. """
        # Pre:
        # For this method to return an item in the table, rather than null, the key must be in the table, i.e.
        # ContainsKey(Key) must return true.
        # Post:
        # The return value of this method will be the item that corresponds with the key, or None, if it is not found.
        # It is recommended to check if the table contains the key by using ContainsKey.
        return self.tree.retrieve(Key)

    @property
    def key_map(self):
        """ Gets the mapping function that maps list items to their search keys. """
        return self.tree.key_map

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        return self.tree.count

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates whether the table is empty or not. """
        return self.tree.is_empty