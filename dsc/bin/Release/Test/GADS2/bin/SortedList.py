from ISortedList import *

class SortedList(ISortedList):
    """ A straightforward implementation of a sorted list that uses a random-access list as backing storage and utilizes a mapping function to compare items. """
    # Remarks:
    # This 'ISortedList<T>' implementation can be used to implement selection sort when used in conjunction with a 'SortedListSort<T>' instance.

    def __init__(self, KeyMap, List):
        """ Creates a new sorted list from the given mapping function and list. """
        self.key_map = KeyMap
        self.list = List

    def add(self, Item):
        """ Adds an item to the collection. """
        index = 0
        key = self.key_map.map(Item)
        for listItem in self.list:
            if self.key_map.map(listItem) < key:
                index += 1
        self.list.insert(index, Item)

    def remove(self, Item):
        """ Removes an item from the list. """
        # Pre:
        # For Item to be successfully removed from the sorted list, it must have been in the list before removal.
        # Post:
        # Returns true if the sorted list contained the given item, false if not.
        # If this returns true, the item has been removed from the sorted list.
        index = -1
        key = self.key_map.map(Item)
        i = 0
        for listItem in self.list:
            if self.key_map.map(listItem) == key:
                index = i
                break
            i += 1
        if index < 0:
            return False
        else:
            self.list.remove_at(index)
            return True

    def contains(self, Item):
        """ Finds out if the sorted list contains the given item. """
        key = self.key_map.map(Item)
        for listItem in self.list:
            if self.key_map.map(listItem) == key:
                return True
        return False

    def to_list(self):
        """ Returns a read-only list that represents this list's contents, for easy enumeration. """
        return self.list

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        return self.list.__iter__()

    @property
    def key_map(self):
        """ Gets a record-to-key map that maps records to sortable keys. """
        return self.key_map_value

    @key_map.setter
    def key_map(self, value):
        """ 
            This accessor is private. """
        self.key_map_value = value

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates if the sorted list is empty. """
        # Post:
        # Return true if empty, false if not.
        return self.list.count == 0

    @property
    def count(self):
        """ Gets the number of elements in the collection. """
        return self.list.count