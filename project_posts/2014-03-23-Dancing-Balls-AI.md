I've decided that my goal will be to use a combination of an incoming audio source and physical input from patrons, to encourage the balls to move in a graceful manner.

I'll be starting with a simple AI routine, just getting the balls to move in an "aimless" emotionally negative way. The idea being that if nobody is interacting with them, they look despondent and sad.

Once people start playing with them, they become a generative art piece in their own right.

![Balls-initial](../project_images/Balls-initial.png?raw=true "Balls-initial")

Currently I'm using a combination of Rigidbody physics and straight transforming of position logic, which is resulting in poor movement and shakes.

I think I will next move to purely using the internal physics engine, using a method sort of similar to imagining there was a rocket on each ball, that can be positioned based on where it wants to go.

The ability of the ball to track to it's desired path, with strength and accuracy, should give a more realistic organic look, as if it was having to struggle to keep it moving on the right path.

I will also have to investigate setting up splines, for the balls to know as their desired path. This will also produce the angle of the rocket to fire to follow the path.

