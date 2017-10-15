// Mostly this is built directly from [source-sdk-2013](https://github.com/ValveSoftware/source-sdk-2013/blob/56accfdb9c4abd32ae1dc26b2e4cc87898cf4dc1/sp/src/game/shared/gamemovement.cpp)
// Though there's quite a few edits or adjustments to make it work with unity's character controller.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SourcePlayer : MonoBehaviour {
	public GameObject deathSpawn;
	public Vector3 gravity = new Vector3(0,-20f,0); // gravity in meters per second per second.
	public float baseFriction = 6f; // A friction multiplier, higher means more friction.
	public float maxSpeed = 35f; // The maximum speed the player can move at.
	public float groundAccelerate = 10f; // How fast we accelerate while on solid ground.
	public float groundDecellerate = 10f; // How fast we deaccelerate on solid ground.
	public float airAccelerate = 1f; // How much air control the player has.
	public float airDeccelerate = 10f; // How fast the player can stop in mid-air or slow down.
	public float walkSpeed = 10f; // How fast the player runs.
	public float jumpSpeed = 8f; // The y velocity to set our character at when they jump.
	public float fallPunchThreshold = 8f; // How fast we must be falling before we shake the screen and make a thud.
	public float maxSafeFallSpeed = 15f; // How fast we must be falling before we take damage.
	public float jumpSpeedBonus = 0.1f; // Speed boost from just jumping forward as a percentage.
	public float health = 100f;
	// We only collide with these layers.
	private int layerMask;
	private float lastGrunt;
	private float stepSize = 0.2f;
	private float fallVelocity;
	public Vector3 groundVelocity;
	private Rigidbody body;
	private float groundFriction;
	private AudioSource jumpGrunt;
	private AudioSource painGrunt;
	private AudioSource hardLand;
	public Vector3 halfExtents = new Vector3(0.5f,1f,0.5f);
	public Vector3 velocity;
	private List<TouchInfo> touched;
	private class TouchInfo {
		public RaycastHit hit;
		public Vector3 vel;
		public TouchInfo(RaycastHit hitinfo, Vector3 velinfo) {
			hit = hitinfo;
			vel = velinfo;
		}
	}
	public bool isGrounded;
	public bool wallblocked;
	public GameObject ground;
	public Vector3 groundNormal;
	void Start() {
		body = GetComponent<Rigidbody> ();
		groundFriction = 1f;
		touched = new List<TouchInfo> ();
		var aSources = GetComponents<AudioSource> ();
		jumpGrunt = aSources [0];
		painGrunt = aSources [1];
		hardLand = aSources [2];
		// This generates our layermask, making sure we only collide with stuff that's specified by the physics engine.
		int myLayer = gameObject.layer;
		layerMask = 0;
		for(int i = 0; i < 32; i++) {
			if(!Physics.GetIgnoreLayerCollision(myLayer, i))  {
				layerMask = layerMask | 1 << i;
			}
		}
	}

	void Update() {
		groundVelocity = GetGroundVelocity ();
		touched.Clear (); // Assume we're not touching anything.

		// If we are not on ground, store off how fast we are moving down
		if ( ground == null  && velocity.y <= 0) {
			fallVelocity = -velocity.y;
		}
		FullPlayerMove ();
		Unstick (2);
	}
	// This command, in a nutshell, scales player input in order to take into account sqrt(2) distortions
	// from walking diagonally. It also multiplies the answer by the walkspeed for convenience.
	private Vector3 GetCommandVelocity() {
		float max;
		float total;
		float scale;
		Vector3 command = new Vector3 (Input.GetAxisRaw ("Horizontal"), 0, Input.GetAxisRaw ("Vertical"));

		max = Mathf.Max (Mathf.Abs(command.z), Mathf.Abs(command.x));
		if (max <= 0) {
			return new Vector3 (0f, 0f, 0f);
		}

		total = Mathf.Sqrt(command.z * command.z + command.x * command.x);
		scale = max / total;

		return command*scale*walkSpeed;
	}
	private void Gravity() {
		velocity += gravity * Time.deltaTime;
	}
	private void CheckJump() {
		// Check to make sure we have a ground under us, and that it's stable ground.
		if ( Input.GetButton("Jump") && ground && groundNormal.y > 0.7f ) {
			// Play a grunt sound, but only so often.
			if (Time.time - lastGrunt > 0.3) {
				jumpGrunt.Play ();
				lastGrunt = Time.time;
			}
			velocity.y = jumpSpeed;
			ground = null;
			velocity += groundVelocity;
			groundVelocity = Vector3.zero;
			// We give a certain percentage of the current forward movement as a bonus to the jump speed.  That bonus is clipped
			// to not accumulate over time
			Vector3 commandVel = GetCommandVelocity ();
			float flSpeedAddition = Mathf.Abs( commandVel.z * jumpSpeedBonus );
			float flMaxSpeed = maxSpeed + ( maxSpeed * jumpSpeedBonus );
			Vector3 flatvel = new Vector3( velocity.x, 0, velocity.z );
			float flNewSpeed = ( flSpeedAddition + flatvel.magnitude );
			// If we're over the maximum, we want to only boost as much as will get us to the goal speed
			if ( flNewSpeed > flMaxSpeed ) {
				flSpeedAddition -= flNewSpeed - flMaxSpeed;
			}
			if (commandVel.z < 0.0f) {
				flSpeedAddition *= -1.0f;
			}
			velocity += transform.forward * flSpeedAddition;
		}
	}
	private void CheckFalling() {
		//Debug.Log (fallVelocity);
		// this function really deals with landing, not falling, so early out otherwise
		if (ground == null || fallVelocity <= 0f) {
			return;
		}

		// We landed on something solidly, if it has some velocity we need to subtract it from our own.
		// This makes our velocities match up again.
		if (groundVelocity.magnitude > 0) {
			velocity -= groundVelocity;
		}

		if ( fallVelocity >= fallPunchThreshold ) {
				
			//bool bAlive = true;
			//float fvol = 0.5f;

			// Scale it down if we landed on something that's floating...
			//if ( player->GetGroundEntity()->IsFloating() ) {
			//	player->m_Local.m_flFallVelocity -= PLAYER_LAND_ON_FLOATING_OBJECT;
			//}

			//
			// They hit the ground.
			//

			// Player landed on a descending object. Subtract the velocity of the ground entity.
			if (groundVelocity.y < 0f) {
				fallVelocity += groundVelocity.y;
				fallVelocity = Mathf.Max (0.1f, fallVelocity);
			}

			if ( fallVelocity > maxSafeFallSpeed ) {
				//
				// If they hit the ground going this fast they may take damage (and die).
				//
				hardLand.Play ();
				//gameObject.SendMessage("Damage", (fallVelocity - maxSafeFallSpeed)*5f );
				//fvol = 1.0f;
			} else if ( fallVelocity > maxSafeFallSpeed / 2 ) {
				//fvol = 0.85f;
			} else {
				//fvol = 0f;
			}

			// PlayerRoughLandingEffects( fvol );
		}

		//
		// Clear the fall velocity so the impact doesn't happen again.
		//
		fallVelocity = 0;
	}
	private void Friction() {
		float	speed, newspeed, control;
		float	friction;
		float	drop;

		// Calculate speed
		speed = velocity.magnitude;

		// If too slow, return
		if (speed < 0.001f) {
			return;
		}

		drop = 0;
		// apply ground friction
		if (ground != null && !Input.GetButton("Jump")) { // On an entity that is the ground
			friction = baseFriction * groundFriction;

			// Bleed off some speed, but if we have less than the bleed
			//  threshold, bleed the threshold amount.
			control = (speed < groundDecellerate) ? groundDecellerate : speed;

			// Add the amount to the drop amount.
			drop += control*friction*Time.deltaTime;
		}

		// scale the velocity
		newspeed = speed - drop;
		if (newspeed < 0) {
			newspeed = 0;
		}

		if ( newspeed != speed ) {
			// Determine proportion of old speed we are using.
			newspeed /= speed;
			// Adjust velocity according to proportion.
			velocity *= newspeed;
		}

		 // mv->m_outWishVel -= (1.f-newspeed) * mv->m_vecVelocity; // ???
	}
	private void CheckVelocity() {
		int i;
		for (i=0; i < 3; i++) {
			// See if it's bogus.
			if (float.IsNaN(velocity[i])){
				Debug.Log ("Got a NaN velocity.");
				velocity[i] = 0;
			}
		}
		if (velocity.magnitude > maxSpeed) {
			velocity = Vector3.Normalize (velocity) * maxSpeed;
		}
	}

	private void CategorizePosition() {
		Vector3 point;
		RaycastHit pm;

		// if the player hull point one unit down is solid, the player
		// is on ground

		// see if standing on something solid	

		float flOffset = 0.1f + halfExtents.y;

		point = transform.position - new Vector3 (0f, flOffset, 0f);

		Vector3 bumpOrigin;
		bumpOrigin = transform.position;

		// Shooting up really fast.  Definitely not on ground.
		// On ladder moving up, so not on ground either
		// NOTE: 8 is a jump.
		float NON_JUMP_VELOCITY = 7f;

		float zvel = velocity.y;
		bool bMovingUp = zvel > 0.0f;
		bool bMovingUpRapidly = zvel > NON_JUMP_VELOCITY;
		float flGroundEntityVelZ = 0.0f;
		if ( bMovingUpRapidly )
		{
			// Tracker 73219, 75878:  ywb 8/2/07
			// After save/restore (and maybe at other times), we can get a case where we were saved on a lift and 
			//  after restore we'll have a high local velocity due to the lift making our abs velocity appear high.  
			// We need to account for standing on a moving ground object in that case in order to determine if we really 
			//  are moving away from the object we are standing on at too rapid a speed.  Note that CheckJump already sets
			//  ground entity to NULL, so this wouldn't have any effect unless we are moving up rapidly not from the jump button.
			if ( ground != null ) {
				flGroundEntityVelZ = groundVelocity.y;
				bMovingUpRapidly = ( zvel - flGroundEntityVelZ ) > NON_JUMP_VELOCITY;
			}
		}

		// Was on ground, but now suddenly am not
		if ( bMovingUpRapidly ) {
			ground = null;
		} else {
			// Try and move down.
			float fraction;
			Vector3 outpos;
			bool hit = TryTouchGround(bumpOrigin, point, out pm, out fraction, out outpos);
			
			// Was on ground, but now suddenly am not.  If we hit a steep plane, we are not on ground
			if ( !hit || pm.normal.y < 0.7 ) {
				// Test four sub-boxes, to see if any of them would have found shallower slope we could actually stand on
				//TryTouchGroundInQuadrants( bumpOrigin, point, out pm, out fraction, out outpos );

				if ( fraction == 1f || pm.normal.y < 0.7 ) {
					ground = null;
				} else {
					ground = pm.collider.gameObject;
					groundNormal = pm.normal;
				}
			} else {
				ground = pm.collider.gameObject;
				groundNormal = pm.normal;
			}
		}
	}
	/*private void TryTouchGroundInQuadrants( Vector3 start, Vector3 end, out RaycastHit pm, out float fraction, out Vector3 endpos) {
		float saveFraction = fraction;
		float saveEndPos = endpos;
		Vector3 quadExtents = halfExtents / 2f;
		quadExtents.y = 0f;
		// Check the -x, -y quadrant
		bool hit = TracePlayerBBoxQuadrant( start-quadExtents, end-quadExtents, out pm, out fraction, out endpos);
		if ( hit && pm.normal.y >= 0.7)
		{
			fraction = saveFraction;
			endpos = saveEndPos;
			return;
		}

		// Check the +x, +y quadrant
		hit = TracePlayerBBoxQuadrant( start+quadExtents, end+quadExtents, out pm, out fraction, out endpos);
		if ( hit && pm.normal.y >= 0.7)
		{
			fraction = saveFraction;
			endpos = saveEndPos;
			return;
		}

		// Check the -x, +y quadrant
		hit = TracePlayerBBoxQuadrant( start+Vector3(-quadExtents.x, 0f, quadExtents.z), end+Vector3(-quadExtents.x, 0f, quadExtents.z), out pm, out fraction, out endpos);
		if ( hit && pm.normal.y >= 0.7)
		{
			fraction = saveFraction;
			endpos = saveEndPos;
			return;
		}

		// Check the +x, -y quadrant
		hit = TracePlayerBBoxQuadrant( start+Vector3(quadExtents.x, 0f, -quadExtents.z), end+Vector3(quadExtents.x, 0f, -quadExtents.z), out pm, out fraction, out endpos);
		if ( hit && pm.normal.y >= 0.7)
		{
			fraction = saveFraction;
			endpos = saveEndPos;
			return;
		}

		fraction = saveFraction;
		endpos = saveEndPos;
	}*/
	private void FullPlayerMove() {
		Gravity ();
		CheckFalling();
		// Was jump button pressed?
		CheckJump();
		// Make sure we're standing on solid ground
		if (groundNormal.y < 0.7f) {
			ground = null;
		}
		// Friction is handled before we add in any base velocity. That way, if we are on a conveyor, 
		//  we don't slow when standing still, relative to the conveyor.
		if (ground != null) {
			velocity.y = 0;
			Friction();
		}

		// Make sure velocity is valid.
		CheckVelocity();

		if (ground != null) {
			WalkMove();
		} else {
			AirMove();  // Take into account movement when in air.
		}

		// Set final flags.
		CategorizePosition();

		// Make sure velocity is valid.
		CheckVelocity();

		// If we are on ground, no downward velocity.
		if ( ground != null ) {
			velocity.y = 0;
		}
	}
	private void Accelerate( Vector3 wishdir, float wishspeed, float accel )
	{
		//int i;
		float addspeed, accelspeed, currentspeed;

		// This gets overridden because some games (CSPort) want to allow dead (observer) players
		// to be able to move around.
		//if ( !CanAccelerate() )
		//	return;

		// Cap speed
		if (wishspeed > maxSpeed) {
			wishspeed = maxSpeed;
		}

		// See if we are changing direction a bit
		currentspeed = Vector3.Dot(velocity, wishdir);

		// Reduce wishspeed by the amount of veer.
		addspeed = wishspeed - currentspeed;

		// If not going to add any speed, done.
		if (addspeed <= 0) {
			return;
		}

		// Determine amount of accleration.
		accelspeed = accel * Time.deltaTime * wishspeed * groundFriction;

		// Cap at addspeed
		if (accelspeed > addspeed) {
			accelspeed = addspeed;
		}

		velocity += accelspeed * wishdir;
	}
	private void StayOnGround() {
		RaycastHit trace;
		Vector3 start = transform.position;
		Vector3 end = transform.position;
		start.y += stepSize;
		end.y -= stepSize;

		// See how far up we can go without getting stuck

		float fraction;
		Vector3 hitpos;
		TracePlayerBBox( transform.position, start, out trace, out fraction, out hitpos);
		start = hitpos;

		// using trace.startsolid is unreliable here, it doesn't get set when
		// tracing bounding box vs. terrain

		// Now trace down from a known safe position
		TracePlayerBBox( start, end, out trace, out fraction, out hitpos );
		if ( fraction > 0.0f &&			// must go somewhere
			 fraction < 1.0f &&			// must hit something
			//!trace.startsolid &&		// can't be embedded in a solid
			 trace.normal.y >= 0.7 )	// can't hit a steep slope that we can't stand on anyway
		{
			transform.position = hitpos;
		}
	}
	private void WalkMove() {
		//int i;
		Vector3 wishvel;
		float spd;
		float fmove, smove;
		Vector3 wishdir;
		float wishspeed;

		//Vector3 dest;
		Vector3 forward, right, up;

		//AngleVectors (mv->m_vecViewAngles, &forward, &right, &up);  // Determine movement angles
		forward = transform.forward;
		right = transform.right;
		up = transform.up;

		// Copy movement amounts
		Vector3 command = GetCommandVelocity();
		fmove = command.z; // Forward/backward
		smove = command.x; // Left/right

		// Zero out z components of movement vectors
		forward.y = 0;
		right.y   = 0;

		forward = Vector3.Normalize (forward);  // Normalize remainder of vectors.
		right = Vector3.Normalize (right);    // 

		// Determine x and y parts of velocity
		wishvel = forward * fmove + right * smove;
		wishvel.y = 0;             // Zero out z part of velocity

		wishdir = new Vector3 (wishvel.x, wishvel.y, wishvel.z); // Determine maginitude of speed of move
		wishspeed = wishdir.magnitude;
		wishdir = Vector3.Normalize(wishdir);

		//
		// Clamp to server defined max speed
		//
		if ((wishspeed != 0.0f) && (wishspeed > maxSpeed))
		{
			wishvel *= maxSpeed / wishspeed;
			wishspeed = maxSpeed;
		}

		// Set pmove velocity
		velocity.y = 0;
		Accelerate ( wishdir, wishspeed, groundAccelerate );
		velocity.y = 0;

		// Add in any base velocity to the current velocity.
		//VectorAdd (mv->m_vecVelocity, player->GetBaseVelocity(), mv->m_vecVelocity );
		velocity += groundVelocity;

		spd = velocity.magnitude;

		if ( spd < 0.01f ) {
			velocity = new Vector3 (0, 0, 0);
			// Now pull the base velocity back out.   Base velocity is set if you are on a moving object, like a conveyor (or maybe another monster?)
			// VectorSubtract( mv->m_vecVelocity, player->GetBaseVelocity(), mv->m_vecVelocity );
			return;
		}

		// First just try moving directly to the location.
		Vector3 dest = transform.position + new Vector3(velocity.x,0,velocity.z) * Time.deltaTime;
		RaycastHit pm;
		float fraction;
		Vector3 hitpos;
		bool hit = TracePlayerBBox (transform.position, dest, out pm, out fraction, out hitpos);
		if (!hit) {
			transform.position = hitpos;
			velocity -= groundVelocity;
			StayOnGround();
			return;
		}
		//StepMove (dest, pm);
		TryPlayerMove();
		// Now pull the base velocity back out.   Base velocity is set if you are on a moving object, like a conveyor (or maybe another monster?)
		// VectorSubtract( mv->m_vecVelocity, player->GetBaseVelocity(), mv->m_vecVelocity );
		velocity -= groundVelocity;
		StayOnGround();
	}
	private void AirMove() {
		//int			i;
		Vector3		wishvel;
		float		fmove, smove;
		Vector3		wishdir;
		float		wishspeed;
		Vector3 	forward, right, up;

		//AngleVectors (mv->m_vecViewAngles, &forward, &right, &up);  // Determine movement angles
		forward = transform.forward;
		right = transform.right;
		up = transform.up;

		// Copy movement amounts
		Vector3 command = GetCommandVelocity();
		fmove = command.z; // Forward/backward
		smove = command.x; // Left/right

		// Zero out up/down components of movement vectors
		forward.y = 0;
		right.y = 0;
		Vector3.Normalize(forward);  // Normalize remainder of vectors
		Vector3.Normalize(right);    // 

		wishvel = forward * fmove + right * smove;
		wishvel.y = 0;             // Zero out up/down part of velocity

		wishdir = new Vector3 (wishvel.x, wishvel.y, wishvel.z);
		wishspeed = wishdir.magnitude;
		wishdir = Vector3.Normalize(wishdir);

		//
		// clamp to server defined max speed
		//
		if ( wishspeed != 0 && (wishspeed > maxSpeed)) {
			wishvel = wishvel * maxSpeed/wishspeed;
			wishspeed = maxSpeed;
		}

		// If we're trying to stop, use airDeccelerate value (usually much larger value than airAccelerate)
		if (Vector3.Dot (velocity, wishdir) < 0) {
			Accelerate (wishdir, wishspeed, airDeccelerate);
		} else {
			Accelerate (wishdir, wishspeed, airAccelerate);
		}

		// Add in any base velocity to the current velocity.
		//VectorAdd(mv->m_vecVelocity, player->GetBaseVelocity(), mv->m_vecVelocity );
		velocity += groundVelocity;
		//TryPlayerMove();
		//controller.Move(velocity * Time.deltaTime);
		TryPlayerMove();

		velocity -= groundVelocity;

		// Now pull the base velocity back out.   Base velocity is set if you are on a moving object, like a conveyor (or maybe another monster?)
		//VectorSubtract( mv->m_vecVelocity, player->GetBaseVelocity(), mv->m_vecVelocity );
	}
	void Damage( float damage ) {
		health -= damage;
		painGrunt.Play ();
		if (health <= 0f) {
			gameObject.SetActive (false);
			Instantiate (deathSpawn, transform.position, Quaternion.identity);
		}
	}
	public Vector3 GetGroundVelocity() {
		if ( ground == null ) {
			return Vector3.zero;
		}
		Rigidbody check = ground.GetComponent<Rigidbody> ();
		if (check != null) {
			return check.GetPointVelocity (transform.position - new Vector3 (0f, halfExtents.y, 0f));
		}
		Movable ccheck = ground.GetComponent<Movable> ();
		if ( ccheck != null ) {
			return ccheck.velocity;
		}
		return Vector3.zero;
	}
	private void UnstickFrom(Collider col, int iter) {
		if ( iter <= 0f ) {
			return;
		}
		// Find our deepest embedded corner.
		List<Vector3> points = new List<Vector3>();
		points.Add (new Vector3 (halfExtents.x, halfExtents.y, halfExtents.z));
		points.Add (new Vector3 (halfExtents.x, halfExtents.y, -halfExtents.z));
		points.Add (new Vector3 (halfExtents.x, -halfExtents.y, -halfExtents.z));
		points.Add (new Vector3 (halfExtents.x, -halfExtents.y, halfExtents.z));
		points.Add (new Vector3 (-halfExtents.x, halfExtents.y, halfExtents.z));
		points.Add (new Vector3 (-halfExtents.x, halfExtents.y, -halfExtents.z));
		points.Add (new Vector3 (-halfExtents.x, -halfExtents.y, -halfExtents.z));
		points.Add (new Vector3 (-halfExtents.x, -halfExtents.y, halfExtents.z));
		List<Vector3> possibleMoves = new List<Vector3>();
		foreach (Vector3 point in points) {
			//Vector3 worldPoint = transform.TransformPoint (point);
			Vector3 worldPoint = transform.position + point;
			// Make sure we're intersecting already.
			if ( col.ClosestPoint(worldPoint) == worldPoint ) {
				// Add the movement delta to get that point outside of the surface.
				Vector3 surfacePoint;
				SuperCollider.ClosestPointOnSurface(col,worldPoint,1f,out surfacePoint);
				possibleMoves.Add (surfacePoint - worldPoint);
			}
		}
		Vector3 biggestDelta = Vector3.zero;
		// Now select the biggest move, since that's probably our deepest embedded corner.
		foreach (Vector3 delta in possibleMoves) {
			if (delta.magnitude > biggestDelta.magnitude) {
				biggestDelta = delta;
			}
		}
		// Ok now we move one corner out.
		transform.position += biggestDelta;
		// We try a few more times to ensure other corners aren't stuck.
		UnstickFrom (col, iter - 1);
	}
	private void Unstick(int iter) {
		// We're in something solid, we have to completely move ourselves outside of it, otherwise TracePlayerBBox won't detect it.
		foreach( Collider col in Physics.OverlapBox (transform.position, halfExtents, Quaternion.identity, layerMask, QueryTriggerInteraction.Ignore) ) {
			UnstickFrom (col, iter);
		}
	}
	public bool TracePlayerBBox( Vector3 start, Vector3 end, out RaycastHit hit, out float fraction, out Vector3 endpos ) {
		if (Physics.OverlapBox (transform.position, halfExtents, Quaternion.identity, layerMask, QueryTriggerInteraction.Ignore).Length > 0f) {
			Debug.Log ("Stuck..." + Time.time);
		}
		Vector3 direction = Vector3.Normalize (end - start);
		float maxDistance = (end - start).magnitude;
		bool hitSomething = false;
		// Otherwise we can just output what we got.
		hitSomething = Physics.BoxCast (start, halfExtents, direction, out hit, Quaternion.identity, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
		if (hitSomething) {
			fraction = hit.distance / maxDistance;
			endpos = start + direction * hit.distance;
			return hitSomething;
		}
		//hit.distance = maxDistance;
		//hit.normal = Vector3.zero;
		//hit.point = endpos;
		fraction = 1f;
		endpos = end;
		return hitSomething;
	}
	public bool TryTouchGround( Vector3 start, Vector3 end, out RaycastHit hit, out float fraction, out Vector3 endpos ) {
		//Vector3 savetransform = body.transform.position;
		//body.transform.position = start;
		//bool retval = body.SweepTest (Vector3.Normalize (end - start), out hit, (end - start).magnitude, QueryTriggerInteraction.Ignore);
		//body.transform.position = savetransform;
		Vector3 safeExtents = new Vector3( halfExtents.x, 0.1f, halfExtents.y);
		Vector3 safeExtentsOffset = new Vector3 (0f, -halfExtents.y+stepSize, 0f);
		bool retval = Physics.BoxCast (start, safeExtents, Vector3.Normalize (end - start), out hit, Quaternion.identity, (end - start).magnitude+safeExtentsOffset.magnitude, layerMask, QueryTriggerInteraction.Ignore);
		if (retval) {
			fraction = (hit.distance-safeExtentsOffset.magnitude) / (end - start).magnitude;
			endpos = start + Vector3.Normalize (end - start) * hit.distance;
			return retval;
		}
		endpos = end;
		fraction = 1f;
		hit.distance = (end - start).magnitude;
		hit.point = end;
		hit.normal = Vector3.zero;
		return retval;
	}
	/* public bool TracePlayerBBoxQuadrant( Vector3 start, Vector3 end, out RaycastHit hit, out float fraction, out Vector3 endpos ) {
		Vector3 quadExtents = new Vector3 (halfExtents.x / 2f, halfExtents.y, halfExtents.z / 2f);
		bool retval = Physics.BoxCast (start, quadExtents, Vector3.Normalize (end - start), out hit, Quaternion.identity, (end - start).magnitude, layerMask, QueryTriggerInteraction.Ignore);
		if (retval) {
			fraction = hit.distance / (end - start).magnitude;
			endpos = start + Vector3.Normalize (end - start) * hit.distance;
			return retval;
		}
		endpos = end;
		fraction = 1f;
		hit.distance = (end - start).magnitude;
		hit.point = end;
		hit.normal = Vector3.zero;
		return retval;
	} */
	// Slide off of impacting surface
	private Vector3 ClipVelocity( Vector3 vel, Vector3 normal, float overbounce ) {
		float	backoff;
		Vector3	change;
		float   angle;
		int		i, blocked;
		Vector3 outvel;

		angle = normal.y;
		// Determine how far along plane to slide based on incoming direction.
		backoff = Vector3.Dot(vel, normal) * overbounce;

		change = normal*backoff;
		outvel = vel - change;
		// iterate once to make sure we aren't still moving through the plane
		float adjust = Vector3.Dot( outvel, normal );
		if( adjust < 0.0f ) {
			outvel -= ( normal * adjust );
		}
		return outvel;
	}
	public void TryPlayerMove() {
		int				bumpcount, numbumps;
		Vector3			dir;
		float			d;
		//int				numplanes;
		List<Vector3>	planes = new List<Vector3> ();
		Vector3			primal_velocity, original_velocity;
		Vector3 		new_velocity = Vector3.zero;
		int				i, j;
		RaycastHit		pm;
		float 			pm_fraction;
		Vector3			end;
		float			time_left, allFraction;
		int				blocked;
		numbumps  = 4;           // Bump up to four times
		ground = null;
		groundNormal = Vector3.up;
		isGrounded = false; // Assume not blocked.
		wallblocked = false;
		//numplanes = 0;           //  and not sliding along any planes

		original_velocity = velocity; // Store original velocity
		primal_velocity = velocity;
		allFraction = 0;
		time_left = Time.deltaTime;   // Total time for this movement operation.
		for (bumpcount = 0; bumpcount < numbumps; bumpcount++) {
			if (velocity.magnitude == 0.0) {
				break;
			}

			// Assume we can move all the way from the current origin to the
			//  end point.
			end = transform.position + velocity * time_left;
			Vector3 hitpos;
			bool hit = TracePlayerBBox (transform.position, end, out pm, out pm_fraction, out hitpos);
			//Debug.Log (hit + " a " + transform.position + " b " + end + " c " + pm_fraction + " d " + transform.position * (1 - pm_fraction) + end * pm_fraction);
			allFraction += pm_fraction;
			//if (pm.allsolid) {	
			// entity is trapped in another solid
			//VectorCopy (vec3_origin, mv->m_vecVelocity);
			//	return 4;
			//}

			// If we moved some portion of the total distance, then
			//  copy the end position into the pmove.origin and 
			//  zero the plane counter.
			if (pm_fraction > 0) {	
				// actually covered some distance
				transform.position = hitpos;
				//mv->SetAbsOrigin( pm.endpos);
				original_velocity = velocity;
				planes.Clear ();
			}
			// If we covered the entire distance, we are done
			//  and can return.
			if (pm_fraction == 1 || !hit) {
				break;		// moved the entire distance
			}
			// Save entity that blocked us (since fraction was < 1.0)
			//  for contact
			// Add it if it's not already in the list!!!
			//MoveHelper( )->AddToTouched( pm, mv->m_vecVelocity );
			touched.Add(new TouchInfo(pm,velocity) );
			if (pm.normal.y > 0.7f) {
				isGrounded = true;		// floor
				ground = pm.collider.gameObject;
				groundNormal = pm.normal;
			}
			if (pm.normal.y == 0f) {
				wallblocked = true;		// step / wall
			}
			// Reduce amount of m_flFrameTime left by total time left * fraction
			//  that we covered.
			time_left -= time_left * pm_fraction;
			planes.Add (pm.normal);
			// reflect player velocity 
			// Only give this a try for first impact plane because you can get yourself stuck in an acute corner by jumping in place
			//  and pressing forward and nobody was really using this bounce/reflection feature anyway...
			if ( planes.Count == 1 && ground == null) {
				for ( i = 0; i < planes.Count; i++ ) {
					if ( planes[i].y > 0.7  ) {
						// floor or slope
						new_velocity = ClipVelocity( original_velocity, planes[i], 1f );
						original_velocity = new_velocity;
					} else {
						new_velocity = ClipVelocity( original_velocity, planes[i], 1f );
					}
				}

				velocity = new_velocity;
				original_velocity = new_velocity;
			} else {
				for (i=0 ; i < planes.Count; i++) {
					velocity = ClipVelocity (original_velocity,planes[i], 1f);

					for (j = 0; j < planes.Count; j++) {
						if (j != i) {
							// Are we now moving against this plane?
							if (Vector3.Dot (velocity, planes [j]) < 0) {
								break;	// not ok
							}
						}
					}
					if (j == planes.Count) {  // Didn't have to clip, so we're ok
						break;
					}
				}

				// Did we go all the way through plane set
				if (i != planes.Count) {
					// go along this plane
					// pmove.velocity is set in clipping call, no need to set again.
					;  
				} else {	// go along the crease
					if (planes.Count != 2) {
						velocity = Vector3.zero;
						break;
					}
					dir = Vector3.Cross (planes [0], planes [1]);
					dir.Normalize ();
					d = Vector3.Dot(dir,velocity);
					velocity = dir * d;
				}

				//
				// if original velocity is against the original velocity, stop dead
				// to avoid tiny occilations in sloping corners
				//
				d = Vector3.Dot(velocity, primal_velocity);
				if (d <= 0) {
					velocity = Vector3.zero;
					break;
				}
			}
		}
		if (allFraction == 0) {
			velocity = Vector3.zero;
		}
		// Check if they slammed into a wall
		float fSlamVol = 0.0f;

		float fLateralStoppingAmount = primal_velocity.magnitude - velocity.magnitude;
		if ( fLateralStoppingAmount > maxSafeFallSpeed * 2.0f ) {
			fSlamVol = 1.0f;
		} else if ( fLateralStoppingAmount > maxSafeFallSpeed ) {
			fSlamVol = 0.85f;
		}
		//PlayerRoughLandingEffects( fSlamVol );
	}
	//-----------------------------------------------------------------------------
	// Purpose: Does the basic move attempting to climb up step heights.  It uses
	//          the mv->GetAbsOrigin() and mv->m_vecVelocity.  It returns a new
	//          new mv->GetAbsOrigin(), mv->m_vecVelocity, and mv->m_outStepHeight.
	//-----------------------------------------------------------------------------
	public void StepMove( Vector3 vecDestination, RaycastHit trace ) {
		Vector3 vecEndPos;
		vecEndPos = vecDestination;

		// Try sliding forward both on ground and up 16 pixels
		//  take the move that goes farthest
		Vector3 vecPos, vecVel;
		vecPos = transform.position;
		vecVel = velocity;

		// Slide move down.
		TryPlayerMove();

		// Down results.
		Vector3 vecDownPos, vecDownVel;
		vecDownPos = transform.position;
		vecDownVel = velocity;

		// Reset original values.
		transform.position = vecPos;
		velocity = vecVel;

		// Move up a stair height.
		vecEndPos = transform.position;
		vecEndPos.z += stepSize;

		float fraction;
		Vector3 hitpos;
		TracePlayerBBox( transform.position, vecEndPos, out trace, out fraction, out hitpos);
		transform.position = hitpos;

		// Slide move up.
		TryPlayerMove();

		// Move down a stair (attempt to).
		vecEndPos = transform.position;
		vecEndPos.z -= stepSize;

		TracePlayerBBox( transform.position, vecEndPos, out trace, out fraction, out hitpos);

		// If we are not on the ground any more then use the original movement attempt.
		if ( trace.normal.y < 0.7 ) {
			transform.position = vecDownPos;
			velocity = vecDownVel;
			float flStepDist = transform.position.y - vecPos.y;
			if ( flStepDist > 0.0f ) {
				//mv->m_outStepHeight += flStepDist;
			}
			return;
		}

		// If the trace ended up in empty space, copy the end over to the origin.
		if ( fraction == 1f ) {
			transform.position = vecEndPos;
		}

		// Copy this origin to up.
		Vector3 vecUpPos = transform.position;

		// decide which one went farther
		float flDownDist = ( vecDownPos.x - vecPos.x ) * ( vecDownPos.x - vecPos.x ) + ( vecDownPos.z - vecPos.z ) * ( vecDownPos.z - vecPos.z );
		float flUpDist = ( vecUpPos.x - vecPos.x ) * ( vecUpPos.x - vecPos.x ) + ( vecUpPos.z - vecPos.z ) * ( vecUpPos.z - vecPos.z );
		if ( flDownDist > flUpDist ) {
			transform.position = vecDownPos;
			velocity = vecDownVel;
		} else {
			// copy up/down value from slide move
			velocity.y = vecDownVel.y;
		}

		float flStepDista = transform.position.y - vecPos.y;
		if ( flStepDista > 0 )
		{
			//mv->m_outStepHeight += flStepDist;
		}
	}
}