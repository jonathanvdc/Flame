class ITree:
    """ Describes a generic search tree.
        This is a generalization of a binary search tree that is also applicable to 2-3 trees, 2-3-4 trees, black-red trees and AVL trees. """
    # Remarks:
    # Every implementation of ITree<T, TKey> must also implement the '__iter__' method, which is implied by the 'iterable<T>' base type.
    # See the 'IReadOnlyCollection<T>' contract for a detailed explanation of what 'iterable<T>' entails.

    def insert(self, Item):
        """ Inserts an item in the search tree. """
        # Post:
        # The item will be added to the tree, regardless of whether the tree already contains an item with the same search key.
        raise NotImplementedError("Method 'ITree.insert' was not implemented.")

    def retrieve(self, Key):
        """ Retrieves the item with the specified key. """
        # Post:
        # If the search tree contains an item with the specified key, said item is returned.
        # If not, None is returned.
        raise NotImplementedError("Method 'ITree.retrieve' was not implemented.")

    def remove(self, Key):
        """ Removes the item with the specified key from the search tree. """
        # Post:
        # If the search tree contains an item with the provided key, it is removed, and true is returned.
        # Otherwise, the tree's state remains unchanged, and false is returned.
        raise NotImplementedError("Method 'ITree.remove' was not implemented.")

    def traverse_inorder(self):
        """ Performs inorder traversal on the binary search tree and writes its items to a new list. """
        # Post:
        # This method returns a read-only list with element type 'T' that can be used for iteration.
        raise NotImplementedError("Method 'ITree.traverse_inorder' was not implemented.")

    def __iter__(self):
        """ Creates an iterator that iterates over every element in the tree. """
        raise NotImplementedError("Method 'ITree.__iter__' was not implemented.")

    @property
    def key_map(self):
        """ Gets the function that maps the search tree's records to their search keys. """
        raise NotImplementedError("Getter of property 'ITree.key_map' was not implemented.")

    @property
    def count(self):
        """ Counts the number of items in the search tree. """
        raise NotImplementedError("Getter of property 'ITree.count' was not implemented.")

    @property
    def is_empty(self):
        """ Gets a boolean value that indicates if the tree is empty. """
        raise NotImplementedError("Getter of property 'ITree.is_empty' was not implemented.")