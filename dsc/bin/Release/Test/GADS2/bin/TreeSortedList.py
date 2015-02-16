from ISortedList import *

class TreeSortedList(ISortedList):
    """ Describes a tree implementation of a sorted list. """

    def __init__(self, tree):
        """ Creates a new search tree implementation of a sorted list, using the provided tree as backing storage. """
        self.tree = tree

    def add(self, Item):
        """ Adds an item to the collection. """
        self.tree.insert(Item)

    def remove(self, Item):
        """ Removes an item from the list. """
        # Pre:
        # For Item to be successfully removed from the sorted list, it must have been in the list before removal.
        # Post:
        # Returns true if the sorted list contained the given item, false if not.
        # If this returns true, the item has been removed from the sorted list.
        return self.tree.remove(self.key_map.map(Item))

    def contains(self, Item):
        """ Finds out if the sorted list contains the given item. """
        return self.tree.retrieve(self.key_map.map(Item)) is not None

    def to_list(self):
        """ Returns a read-only list that represents this list's contents, for easy enumeration. """
        return self.tree.traverse_inorder()

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the collection. """
        return self.tree.__iter__()

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
        """ Gets a boolean value that indicates whether the sorted list is empty or not. """
        # Post:
        # Return true if empty, false if not.
        return self.tree.is_empty